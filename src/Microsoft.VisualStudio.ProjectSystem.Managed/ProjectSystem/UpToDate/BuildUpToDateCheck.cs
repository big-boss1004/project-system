﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    [AppliesTo(ProjectCapability.DotNet + "+ !" + ProjectCapabilities.SharedAssetsProject)]
    [Export(typeof(IBuildUpToDateCheckProvider))]
    [ExportMetadata("BeforeDrainCriticalTasks", true)]
    internal sealed class BuildUpToDateCheck : OnceInitializedOnceDisposed, IBuildUpToDateCheckProvider
    {
        private const string CopyToOutputDirectory = "CopyToOutputDirectory";
        private const string PreserveNewest = "PreserveNewest";
        private const string Always = "Always";
        private const string Link = "Link";

        private static ImmutableHashSet<string> ReferenceSchemas => ImmutableStringHashSet.EmptyOrdinal
            .Add(ResolvedAnalyzerReference.SchemaName)
            .Add(ResolvedCompilationReference.SchemaName);

        private static ImmutableHashSet<string> UpToDateSchemas => ImmutableStringHashSet.EmptyOrdinal
            .Add(CopyUpToDateMarker.SchemaName)
            .Add(UpToDateCheckInput.SchemaName)
            .Add(UpToDateCheckOutput.SchemaName)
            .Add(UpToDateCheckBuilt.SchemaName);

        private static ImmutableHashSet<string> ProjectPropertiesSchemas => ImmutableStringHashSet.EmptyOrdinal
            .Add(ConfigurationGeneral.SchemaName)
            .Union(ReferenceSchemas)
            .Union(UpToDateSchemas);

        private static ImmutableHashSet<string> NonCompilationItemTypes => ImmutableStringHashSet.EmptyOrdinal
            .Add(None.SchemaName)
            .Add(Content.SchemaName);

        private readonly IProjectSystemOptions _projectSystemOptions;
        private readonly ConfiguredProject _configuredProject;
        private readonly IProjectAsynchronousTasksService _tasksService;
        private readonly IProjectItemSchemaService _projectItemSchemaService;
        private readonly ITelemetryService _telemetryService;

        private IDisposable _link;
        private IComparable _lastVersionSeen;

        private bool _isDisabled = true;
        private bool _itemsChangedSinceLastCheck = true;
        private string _msBuildProjectFullPath;
        private string _msBuildProjectDirectory;
        private string _markerFile;
        private string _outputRelativeOrFullPath;

        private readonly HashSet<string> _imports = new HashSet<string>(StringComparers.Paths);
        private readonly HashSet<string> _itemTypes = new HashSet<string>(StringComparers.Paths);
        private readonly Dictionary<string, HashSet<(string Path, string Link, CopyToOutputDirectoryType CopyType)>> _items = new Dictionary<string, HashSet<(string, string, CopyToOutputDirectoryType)>>();
        private readonly HashSet<string> _customInputs = new HashSet<string>(StringComparers.Paths);
        private readonly HashSet<string> _customOutputs = new HashSet<string>(StringComparers.Paths);
        private readonly HashSet<string> _builtOutputs = new HashSet<string>(StringComparers.Paths);
        private readonly Dictionary<string, string> _copiedOutputFiles = new Dictionary<string, string>();
        private readonly HashSet<string> _analyzerReferences = new HashSet<string>(StringComparers.Paths);
        private readonly HashSet<string> _compilationReferences = new HashSet<string>(StringComparers.Paths);
        private readonly HashSet<string> _copyReferenceInputs = new HashSet<string>(StringComparers.Paths);

        [ImportingConstructor]
        public BuildUpToDateCheck(
            IProjectSystemOptions projectSystemOptions,
            ConfiguredProject configuredProject,
            [Import(ExportContractNames.Scopes.ConfiguredProject)] IProjectAsynchronousTasksService tasksService,
            IProjectItemSchemaService projectItemSchemaService,
            ITelemetryService telemetryService)
        {
            _projectSystemOptions = projectSystemOptions;
            _configuredProject = configuredProject;
            _tasksService = tasksService;
            _projectItemSchemaService = projectItemSchemaService;
            _telemetryService = telemetryService;
        }

        /// <summary>
        /// Called on project load.
        /// </summary>
        [ConfiguredProjectAutoLoad]
        [AppliesTo(ProjectCapability.DotNet + "+ !" + ProjectCapabilities.SharedAssetsProject)]
        internal void Load()
        {
            EnsureInitialized();
        }

        protected override void Initialize()
        {
            _link = ProjectDataSources.SyncLinkTo(
                _configuredProject.Services.ProjectSubscription.JointRuleSource.SourceBlock.SyncLinkOptions(DataflowOption.WithRuleNames(ProjectPropertiesSchemas)),
                _configuredProject.Services.ProjectSubscription.SourceItemsRuleSource.SourceBlock.SyncLinkOptions(),
                _projectItemSchemaService.SourceBlock.SyncLinkOptions(),
                target: new ActionBlock<IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectSubscriptionUpdate, IProjectItemSchema>>>(e => OnChanged(e)),
                linkOptions: DataflowOption.PropagateCompletion);
        }

        private void OnProjectChanged(IProjectSubscriptionUpdate e)
        {
            _isDisabled = e.CurrentState.IsPropertyTrue(ConfigurationGeneral.SchemaName, ConfigurationGeneral.DisableFastUpToDateCheckProperty, defaultValue: false);

            _msBuildProjectFullPath = e.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildProjectFullPathProperty, _msBuildProjectFullPath);
            _msBuildProjectDirectory = e.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildProjectDirectoryProperty, _msBuildProjectDirectory);
            _outputRelativeOrFullPath = e.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.OutputPathProperty, _outputRelativeOrFullPath);

            string[] allProjects = e.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildAllProjectsProperty, string.Empty)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            _imports.Clear();
            _imports.AddRange(allProjects);

            if (e.ProjectChanges.TryGetValue(ResolvedAnalyzerReference.SchemaName, out IProjectChangeDescription changes) &&
                changes.Difference.AnyChanges)
            {
                _analyzerReferences.Clear();
                _analyzerReferences.AddRange(changes.After.Items.Select(item => item.Value[ResolvedAnalyzerReference.ResolvedPathProperty]));
            }

            if (e.ProjectChanges.TryGetValue(ResolvedCompilationReference.SchemaName, out changes) &&
                changes.Difference.AnyChanges)
            {
                _compilationReferences.Clear();
                _copyReferenceInputs.Clear();

                foreach (KeyValuePair<string, IImmutableDictionary<string, string>> item in changes.After.Items)
                {
                    _compilationReferences.Add(item.Value[ResolvedCompilationReference.ResolvedPathProperty]);
                    if (!string.IsNullOrWhiteSpace(item.Value[CopyUpToDateMarker.SchemaName]))
                    {
                        _copyReferenceInputs.Add(item.Value[CopyUpToDateMarker.SchemaName]);
                    }
                    if (!string.IsNullOrWhiteSpace(item.Value[ResolvedCompilationReference.OriginalPathProperty]))
                    {
                        _copyReferenceInputs.Add(item.Value[ResolvedCompilationReference.OriginalPathProperty]);
                    }
                }
            }

            if (e.ProjectChanges.TryGetValue(UpToDateCheckInput.SchemaName, out IProjectChangeDescription inputs) &&
                inputs.Difference.AnyChanges)
            {
                _customInputs.Clear();
                _customInputs.AddRange(inputs.After.Items.Keys);
            }

            if (e.ProjectChanges.TryGetValue(UpToDateCheckOutput.SchemaName, out IProjectChangeDescription outputs) &&
                outputs.Difference.AnyChanges)
            {
                _customOutputs.Clear();
                _customOutputs.AddRange(outputs.After.Items.Keys);
            }

            if (e.ProjectChanges.TryGetValue(UpToDateCheckBuilt.SchemaName, out IProjectChangeDescription built) &&
                built.Difference.AnyChanges)
            {
                _builtOutputs.Clear();

                foreach (KeyValuePair<string, IImmutableDictionary<string, string>> item in built.After.Items)
                {
                    string destination = item.Key;

                    if (item.Value.TryGetValue(UpToDateCheckBuilt.OriginalProperty, out string source) &&
                        !string.IsNullOrEmpty(source))
                    {
                        _copiedOutputFiles[destination] = source;
                    }
                    else
                    {
                        _builtOutputs.Add(destination);
                    }
                }
            }

            if (e.ProjectChanges.TryGetValue(CopyUpToDateMarker.SchemaName, out IProjectChangeDescription upToDateMarkers) &&
                upToDateMarkers.Difference.AnyChanges)
            {
                _markerFile = upToDateMarkers.After.Items.Count == 1 ? _configuredProject.UnconfiguredProject.MakeRooted(upToDateMarkers.After.Items.Single().Key) : null;
            }
        }

        private static string GetLink(IImmutableDictionary<string, string> itemMetadata) =>
            itemMetadata.TryGetValue(Link, out string link) ? link : null;

        private static CopyToOutputDirectoryType GetCopyType(IImmutableDictionary<string, string> itemMetadata)
        {
            if (itemMetadata.TryGetValue(CopyToOutputDirectory, out string value))
            {
                if (string.Equals(value, Always, StringComparison.OrdinalIgnoreCase))
                {
                    return CopyToOutputDirectoryType.CopyAlways;
                }

                if (string.Equals(value, PreserveNewest, StringComparison.OrdinalIgnoreCase))
                {
                    return CopyToOutputDirectoryType.CopyIfNewer;
                }
            }

            return CopyToOutputDirectoryType.CopyNever;
        }

        private void OnSourceItemChanged(IProjectSubscriptionUpdate e, IProjectItemSchema projectItemSchema)
        {
            string[] itemTypes = projectItemSchema.GetKnownItemTypes().Where(itemType => projectItemSchema.GetItemType(itemType).UpToDateCheckInput).ToArray();
            bool itemTypesChanged = !_itemTypes.SetEquals(itemTypes);

            if (itemTypesChanged)
            {
                _itemTypes.Clear();
                _itemTypes.AddRange(itemTypes);
                _items.Clear();
            }

            foreach (KeyValuePair<string, IProjectChangeDescription> itemType in e.ProjectChanges.Where(changes => (itemTypesChanged || changes.Value.Difference.AnyChanges) && _itemTypes.Contains(changes.Key)))
            {
                IEnumerable<(string, string, CopyToOutputDirectoryType)> items = itemType.Value.After.Items
                    .Select(item => (item.Key, GetLink(item.Value), GetCopyType(item.Value)));
                _items[itemType.Key] = new HashSet<(string, string, CopyToOutputDirectoryType)>(items, UpToDateCheckItemComparer.Instance);
                _itemsChangedSinceLastCheck = true;
            }

            if (e.ProjectChanges.TryGetValue(UpToDateCheckOutput.SchemaName, out IProjectChangeDescription outputs) &&
                outputs.Difference.AnyChanges)
            {
                _customOutputs.Clear();
                _customOutputs.AddRange(outputs.After.Items.Keys);
            }
        }

        private void OnChanged(IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectSubscriptionUpdate, IProjectItemSchema>> e)
        {
            OnProjectChanged(e.Value.Item1);
            OnSourceItemChanged(e.Value.Item2, e.Value.Item3);
            _lastVersionSeen = e.DataSourceVersions[ProjectDataSources.ConfiguredProjectVersion];
        }

        protected override void Dispose(bool disposing)
        {
            _link?.Dispose();
        }

        private static DateTime? GetTimestamp(string path, IDictionary<string, DateTime> timestampCache)
        {
            if (!timestampCache.TryGetValue(path, out DateTime time))
            {
                var info = new FileInfo(path);
                if (!info.Exists)
                {
                    return null;
                }
                time = info.LastWriteTimeUtc;
                timestampCache[path] = time;
            }

            return time;
        }

        private bool Fail(BuildUpToDateCheckLogger logger, string message, string reason)
        {
            logger.Info(message);
            _telemetryService.PostProperty(TelemetryEventName.UpToDateCheckFail, TelemetryPropertyName.UpToDateCheckFailReason, reason);
            return false;
        }

        private bool CheckGlobalConditions(BuildAction buildAction, BuildUpToDateCheckLogger logger)
        {
            if (buildAction != BuildAction.Build)
            {
                return false;
            }

            bool itemsChangedSinceLastCheck = _itemsChangedSinceLastCheck;
            _itemsChangedSinceLastCheck = false;

            if (!_tasksService.IsTaskQueueEmpty(ProjectCriticalOperation.Build))
            {
                return Fail(logger, "Critical build tasks are running, not up to date.", "CriticalTasks");
            }

            if (_lastVersionSeen == null || _configuredProject.ProjectVersion.CompareTo(_lastVersionSeen) > 0)
            {
                return Fail(logger, "Project information is older than current project version, not up to date.", "ProjectInfoOutOfDate");
            }

            if (itemsChangedSinceLastCheck)
            {
                return Fail(logger, "The list of source items has changed since the last build, not up to date.", "ItemInfoOutOfDate");
            }

            if (_isDisabled)
            {
                return Fail(logger, "The 'DisableFastUpToDateCheckProperty' property is true, not up to date.", "Disabled");
            }

            string copyAlwaysItemPath = _items.SelectMany(kvp => kvp.Value).FirstOrDefault(item => item.CopyType == CopyToOutputDirectoryType.CopyAlways).Path;

            if (copyAlwaysItemPath != null)
            {
                return Fail(logger, $"Item '{_configuredProject.UnconfiguredProject.MakeRooted(copyAlwaysItemPath)}' has CopyToOutputDirectory set to 'Always', not up to date.", "CopyAlwaysItemExists");
            }

            return true;
        }

        private IEnumerable<string> CollectInputs(BuildUpToDateCheckLogger logger)
        {
            logger.Verbose("Adding project file inputs:");
            logger.Verbose("    '{0}'", _msBuildProjectFullPath);
            yield return _msBuildProjectFullPath;

            if (_imports.Count != 0)
            {
                logger.Verbose("Adding import inputs:");
                foreach (string input in _imports)
                {
                    logger.Verbose("    '{0}'", input);
                    yield return input;
                }
            }

            foreach (KeyValuePair<string, HashSet<(string Path, string Link, CopyToOutputDirectoryType CopyType)>> pair in _items.Where(kvp => !NonCompilationItemTypes.Contains(kvp.Key)))
            {
                if (pair.Value.Count != 0)
                {
                    logger.Verbose("Adding {0} inputs:", pair.Key);

                    foreach (string input in pair.Value.Select(item => _configuredProject.UnconfiguredProject.MakeRooted(item.Path)))
                    {
                        logger.Verbose("    '{0}'", input);
                        yield return input;
                    }
                }
            }

            if (_analyzerReferences.Count != 0)
            {
                logger.Verbose("Adding " + ResolvedAnalyzerReference.SchemaName + " inputs:");
                foreach (string input in _analyzerReferences)
                {
                    logger.Verbose("    '{0}'", input);
                    yield return input;
                }
            }

            if (_compilationReferences.Count != 0)
            {
                logger.Verbose("Adding " + ResolvedCompilationReference.SchemaName + " inputs:");
                foreach (string input in _compilationReferences)
                {
                    logger.Verbose("    '{0}'", input);
                    yield return input;
                }
            }

            if (_customInputs.Count != 0)
            {
                logger.Verbose("Adding " + UpToDateCheckInput.SchemaName + " inputs:");
                foreach (string input in _customInputs.Select(_configuredProject.UnconfiguredProject.MakeRooted))
                {
                    logger.Verbose("    '{0}'", input);
                    yield return input;
                }
            }
        }

        private IEnumerable<string> CollectOutputs(BuildUpToDateCheckLogger logger)
        {
            if (_customOutputs.Count != 0)
            {
                logger.Verbose("Adding " + UpToDateCheckOutput.SchemaName + " outputs:");

                foreach (string output in _customOutputs.Select(_configuredProject.UnconfiguredProject.MakeRooted))
                {
                    logger.Verbose("    '{0}'", output);
                    yield return output;
                }
            }

            if (_builtOutputs.Count != 0)
            {
                logger.Verbose("Adding " + UpToDateCheckBuilt.SchemaName + " outputs:");

                foreach (string output in _builtOutputs.Select(_configuredProject.UnconfiguredProject.MakeRooted))
                {
                    logger.Verbose("    '{0}'", output);
                    yield return output;
                }
            }
        }

        private static (DateTime time, string path) GetLatestInput(IEnumerable<string> inputs, IDictionary<string, DateTime> timestampCache)
        {
            DateTime latest = DateTime.MinValue;
            string latestPath = null;

            foreach (string input in inputs)
            {
                DateTime? time = GetTimestamp(input, timestampCache);

                if (time > latest)
                {
                    latest = time.Value;
                    latestPath = input;
                }
            }

            return (latest, latestPath);
        }

        private static (DateTime? time, string path) GetEarliestOutput(IEnumerable<string> outputs, IDictionary<string, DateTime> timestampCache)
        {
            DateTime? earliest = DateTime.MaxValue;
            string earliestPath = null;

            foreach (string output in outputs)
            {
                DateTime? time = GetTimestamp(output, timestampCache);

                if (time == null)
                {
                    return (null, null);
                }

                if (time < earliest)
                {
                    earliest = time;
                    earliestPath = output;
                }
            }

            return (earliest, earliestPath);
        }

        // Reference assembly copy markers are strange. The property is always going to be present on
        // references to SDK-based projects, regardless of whether or not those referenced projects
        // will actually produce a marker. And an item always will be present in an SDK-based project,
        // regardless of whether or not the project produces a marker. So, basically, we only check
        // here if the project actually produced a marker and we only check it against references that
        // actually produced a marker.
        private bool CheckMarkers(BuildUpToDateCheckLogger logger, IDictionary<string, DateTime> timestampCache)
        {
            if (string.IsNullOrWhiteSpace(_markerFile) || !_copyReferenceInputs.Any())
            {
                return true;
            }

            logger.Verbose("Adding input reference copy markers:");

            foreach (string referenceMarkerFile in _copyReferenceInputs)
            {
                logger.Verbose("    '{0}'", referenceMarkerFile);
            }

            logger.Verbose("Adding output reference copy marker:");
            logger.Verbose("    '{0}'", _markerFile);

            (DateTime inputMarkerTime, string inputMarkerPath) = GetLatestInput(_copyReferenceInputs, timestampCache);
            DateTime? outputMarkerTime = GetTimestamp(_markerFile, timestampCache);

            if (inputMarkerPath != null)
            {
                logger.Info("Latest write timestamp on input marker is {0} on '{1}'.", inputMarkerTime, inputMarkerPath);
            }
            else
            {
                logger.Info("No input markers exist, skipping marker check.");
            }

            if (outputMarkerTime != null)
            {
                logger.Info("Write timestamp on output marker is {0} on '{1}'.", outputMarkerTime, _markerFile);
            }
            else
            {
                logger.Info("Output marker '{0}' does not exist, skipping marker check.", _markerFile);
            }

            if (outputMarkerTime <= inputMarkerTime)
            {
                logger.Info("Input marker is newer than output marker, not up to date.");
            }

            return inputMarkerPath == null || outputMarkerTime == null || outputMarkerTime > inputMarkerTime;
        }

        private bool CheckCopiedOutputFiles(BuildUpToDateCheckLogger logger, IDictionary<string, DateTime> timestampCache)
        {
            foreach (KeyValuePair<string, string> kvp in _copiedOutputFiles)
            {
                string source = _configuredProject.UnconfiguredProject.MakeRooted(kvp.Value);
                string destination = _configuredProject.UnconfiguredProject.MakeRooted(kvp.Key);

                logger.Info("Checking build output file '{0}':", source);

                DateTime? itemTime = GetTimestamp(source, timestampCache);

                if (itemTime != null)
                {
                    logger.Info("    Source {0}: '{1}'.", itemTime, source);
                }
                else
                {
                    logger.Info("Source '{0}' does not exist, not up to date.", source);
                    return false;
                }

                DateTime? outputItemTime = GetTimestamp(destination, timestampCache);

                if (outputItemTime != null)
                {
                    logger.Info("    Destination {0}: '{1}'.", outputItemTime, destination);
                }
                else
                {
                    logger.Info("Destination '{0}' does not exist, not up to date.", destination);
                    return false;
                }

                if (outputItemTime < itemTime)
                {
                    logger.Info("Build output destination is newer than source, not up to date.");
                    return false;
                }
            }

            return true;
        }

        private bool CheckCopyToOutputDirectoryFiles(BuildUpToDateCheckLogger logger, IDictionary<string, DateTime> timestampCache)
        {
            IEnumerable<(string Path, string Link, CopyToOutputDirectoryType CopyType)> items = _items.SelectMany(kvp => kvp.Value).Where(item => item.CopyType == CopyToOutputDirectoryType.CopyIfNewer);

            string outputFullPath = Path.Combine(_msBuildProjectDirectory, _outputRelativeOrFullPath);

            foreach ((string Path, string Link, CopyToOutputDirectoryType CopyType) item in items)
            {
                string path = _configuredProject.UnconfiguredProject.MakeRooted(item.Path);
                string filename = string.IsNullOrEmpty(item.Link) ? path : item.Link;

                if (string.IsNullOrEmpty(filename))
                {
                    continue;
                }

                filename = _configuredProject.UnconfiguredProject.MakeRelative(filename);

                logger.Info("Checking PreserveNewest file '{0}':", path);

                DateTime? itemTime = GetTimestamp(path, timestampCache);

                if (itemTime != null)
                {
                    logger.Info("    Source {0}: '{1}'.", itemTime, path);
                }
                else
                {
                    logger.Info("Source '{0}' does not exist, not up to date.", path);
                    return false;
                }

                string outputItem = Path.Combine(outputFullPath, filename);
                DateTime? outputItemTime = GetTimestamp(outputItem, timestampCache);

                if (outputItemTime != null)
                {
                    logger.Info("    Destination {0}: '{1}'.", outputItemTime, outputItem);
                }
                else
                {
                    logger.Info("Destination '{0}' does not exist, not up to date.", outputItem);
                    return false;
                }

                if (outputItemTime < itemTime)
                {
                    logger.Info("PreserveNewest destination is newer than source, not up to date.");
                    return false;
                }
            }

            return true;
        }

        public async Task<bool> IsUpToDateAsync(BuildAction buildAction, TextWriter logWriter, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            EnsureInitialized();

            LogLevel requestedLogLevel = await _projectSystemOptions.GetFastUpToDateLoggingLevelAsync(cancellationToken);
            var logger = new BuildUpToDateCheckLogger(logWriter, requestedLogLevel, _configuredProject.UnconfiguredProject.FullPath);

            if (!CheckGlobalConditions(buildAction, logger))
            {
                return false;
            }

            // Short-lived cache of timestamp by path
            var timestampCache = new Dictionary<string, DateTime>(StringComparers.Paths);

            // We assume there are fewer outputs than inputs, so perform a full scan of outputs to find the earliest
            (DateTime? outputTime, string outputPath) = GetEarliestOutput(CollectOutputs(logger), timestampCache);

            bool outputsUpToDate = true;

            if (outputTime != null)
            {
                // Search for an input that's either missing or newer than the earliest output.
                // As soon as we find one, we can stop the scan.
                foreach (string input in CollectInputs(logger))
                {
                    DateTime? time = GetTimestamp(input, timestampCache);

                    if (time == null)
                    {
                        logger.Info("Input '{0}' does not exist, not up to date.", input);
                        outputsUpToDate = false;
                        break;
                    }

                    if (time > outputTime)
                    {
                        logger.Info("Input '{0}' is newer ({1}) than earliest output '{2}' ({3}), not up to date.", input, time.Value, outputPath, outputTime.Value);
                        outputsUpToDate = false;
                        break;
                    }
                }
            }
            else
            {
                logger.Info("Output '{0}' does not exist, not up to date.", outputPath);
                outputsUpToDate = false;
            }

            // We are up to date if the earliest output write happened after the latest input write
            bool markersUpToDate = CheckMarkers(logger, timestampCache);
            bool copyToOutputDirectoryUpToDate = CheckCopyToOutputDirectoryFiles(logger, timestampCache);
            bool copiedOutputUpToDate = CheckCopiedOutputFiles(logger, timestampCache);
            bool isUpToDate = outputsUpToDate && markersUpToDate && copyToOutputDirectoryUpToDate && copiedOutputUpToDate;

            if (!markersUpToDate)
            {
                _telemetryService.PostProperty(TelemetryEventName.UpToDateCheckFail, TelemetryPropertyName.UpToDateCheckFailReason, "Marker");
            }
            else if (!outputsUpToDate)
            {
                _telemetryService.PostProperty(TelemetryEventName.UpToDateCheckFail, TelemetryPropertyName.UpToDateCheckFailReason, "Outputs");
            }
            else if (!copyToOutputDirectoryUpToDate)
            {
                _telemetryService.PostProperty(TelemetryEventName.UpToDateCheckFail, TelemetryPropertyName.UpToDateCheckFailReason, "CopyToOutputDirectory");
            }
            else if (!copiedOutputUpToDate)
            {
                _telemetryService.PostProperty(TelemetryEventName.UpToDateCheckFail, TelemetryPropertyName.UpToDateCheckFailReason, "CopyOutput");
            }
            else
            {
                _telemetryService.PostEvent(TelemetryEventName.UpToDateCheckSuccess);
            }

            logger.Info("Project is{0} up to date.", !isUpToDate ? " not" : "");

            return isUpToDate;
        }

        public Task<bool> IsUpToDateCheckEnabledAsync(CancellationToken cancellationToken = default) =>
            _projectSystemOptions.GetIsFastUpToDateCheckEnabledAsync(cancellationToken);
    }
}
