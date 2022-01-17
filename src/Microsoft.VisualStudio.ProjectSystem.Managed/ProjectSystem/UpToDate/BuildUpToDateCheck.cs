﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    [AppliesTo(AppliesToExpression)]
    [Export(typeof(IBuildUpToDateCheckProvider))]
    [Export(typeof(IBuildUpToDateCheckValidator))]
    [Export(typeof(IActiveConfigurationComponent))]
    [ExportMetadata("BeforeDrainCriticalTasks", true)]
    internal sealed partial class BuildUpToDateCheck
        : IBuildUpToDateCheckProvider2,
          IBuildUpToDateCheckValidator,
          IActiveConfigurationComponent,
          IDisposable
    {
        internal const string AppliesToExpression = ProjectCapability.DotNet + " + !" + ProjectCapabilities.SharedAssetsProject;

        internal const string FastUpToDateCheckIgnoresKindsGlobalPropertyName = "FastUpToDateCheckIgnoresKinds";

        internal const string DefaultSetName = "";
        internal const string DefaultKindName = "";

        internal static readonly StringComparer SetNameComparer = StringComparers.ItemNames;
        internal static readonly StringComparer KindNameComparer = StringComparers.ItemNames;

        private static ImmutableHashSet<string> NonCompilationItemTypes => ImmutableHashSet<string>.Empty
            .WithComparer(StringComparers.ItemTypes)
            .Add(None.SchemaName)
            .Add(Content.SchemaName);

        private readonly IUpToDateCheckConfiguredInputDataSource _inputDataSource;
        private readonly IProjectSystemOptions _projectSystemOptions;
        private readonly ConfiguredProject _configuredProject;
        private readonly IProjectAsynchronousTasksService _tasksService;
        private readonly ITelemetryService _telemetryService;
        private readonly IFileSystem _fileSystem;
        private readonly IUpToDateCheckHost _upToDateCheckHost;

        private IImmutableDictionary<string, string> _lastGlobalProperties = ImmutableStringDictionary<string>.EmptyOrdinal;
        private string _lastFailureReason = "";

        private ISubscription _subscription;
        private int _isDisposed;

        [ImportingConstructor]
        public BuildUpToDateCheck(
            IUpToDateCheckConfiguredInputDataSource inputDataSource,
            IProjectSystemOptions projectSystemOptions,
            ConfiguredProject configuredProject,
            [Import(ExportContractNames.Scopes.ConfiguredProject)] IProjectAsynchronousTasksService tasksService,
            ITelemetryService telemetryService,
            IFileSystem fileSystem,
            IUpToDateCheckHost upToDateCheckHost)
        {
            _inputDataSource = inputDataSource;
            _projectSystemOptions = projectSystemOptions;
            _configuredProject = configuredProject;
            _tasksService = tasksService;
            _telemetryService = telemetryService;
            _fileSystem = fileSystem;
            _upToDateCheckHost = upToDateCheckHost;
            _subscription = new Subscription(inputDataSource, configuredProject, upToDateCheckHost);
        }

        public Task ActivateAsync()
        {
            _subscription.EnsureInitialized();

            return Task.CompletedTask;
        }

        public Task DeactivateAsync()
        {
            RecycleSubscription();

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0)
            {
                return;
            }

            RecycleSubscription();
        }

        private void RecycleSubscription()
        {
            ISubscription subscription = Interlocked.Exchange(ref _subscription, new Subscription(_inputDataSource, _configuredProject, _upToDateCheckHost));

            subscription.Dispose();
        }

        private bool CheckGlobalConditions(Log log, DateTime lastCheckedAtUtc, bool validateFirstRun, UpToDateCheckImplicitConfiguredInput state)
        {
            if (!_tasksService.IsTaskQueueEmpty(ProjectCriticalOperation.Build))
            {
                return log.Fail("CriticalTasks", nameof(Resources.FUTD_CriticalBuildTasksRunning));
            }

            if (state.IsDisabled)
            {
                return log.Fail("Disabled", nameof(Resources.FUTD_DisableFastUpToDateCheckTrue));
            }

            if (validateFirstRun && !state.WasStateRestored && lastCheckedAtUtc == DateTime.MinValue)
            {
                // We had no persisted state, and this is the first run. We cannot know if the project is up-to-date
                // or not, so schedule a build.
                return log.Fail("FirstRun", nameof(Resources.FUTD_FirstRun));
            }

            foreach ((_, ImmutableArray<UpToDateCheckInputItem> items) in state.InputSourceItemsByItemType)
            {
                foreach (UpToDateCheckInputItem item in items)
                {
                    if (item.CopyType == CopyType.CopyAlways)
                    {
                        return log.Fail("CopyAlwaysItemExists", nameof(Resources.FUTD_CopyAlwaysItemExists_1), _configuredProject.UnconfiguredProject.MakeRooted(item.Path));
                    }
                }
            }

            return true;
        }

        private bool CheckInputsAndOutputs(Log log, DateTime lastCheckedAtUtc, in TimestampCache timestampCache, UpToDateCheckImplicitConfiguredInput state, HashSet<string>? ignoreKinds, CancellationToken token)
        {
            // UpToDateCheckInput/Output/Built items have optional 'Set' metadata that determine whether they
            // are treated separately or not. If omitted, such inputs/outputs are included in the default set,
            // which also includes other items such as project files, compilation items, analyzer references, etc.

            // First, validate the relationship between inputs and outputs within the default set.
            if (!CheckInputsAndOutputs(CollectDefaultInputs(), CollectDefaultOutputs(), timestampCache, DefaultSetName))
            {
                return false;
            }

            // Second, validate the relationships between inputs and outputs in specific sets, if any.
            foreach (string setName in state.SetNames)
            {
                if (log.Level >= LogLevel.Verbose)
                {
                    log.Verbose(nameof(Resources.FUTD_ComparingInputOutputTimestamps_1), setName);
                    log.Indent++;
                }

                if (!CheckInputsAndOutputs(CollectSetInputs(setName), CollectSetOutputs(setName), timestampCache, setName))
                {
                    return false;
                }

                if (log.Level >= LogLevel.Verbose)
                {
                    log.Indent--;
                }
            }

            // Validation passed
            return true;

            bool CheckInputsAndOutputs(IEnumerable<(string Path, string? ItemType, bool IsRequired)> inputs, IEnumerable<string> outputs, in TimestampCache timestampCache, string setName)
            {
                // We assume there are fewer outputs than inputs, so perform a full scan of outputs to find the earliest.
                // This increases the chance that we may return sooner in the case we are not up to date.
                DateTime earliestOutputTime = DateTime.MaxValue;
                string? earliestOutputPath = null;
                bool hasOutput = false;

                foreach (string output in outputs)
                {
                    token.ThrowIfCancellationRequested();

                    DateTime? outputTime = timestampCache.GetTimestampUtc(output);

                    if (outputTime == null)
                    {
                        return log.Fail("OutputNotFound", nameof(Resources.FUTD_OutputDoesNotExist_1), output);
                    }

                    if (outputTime < earliestOutputTime)
                    {
                        earliestOutputTime = outputTime.Value;
                        earliestOutputPath = output;
                    }

                    hasOutput = true;
                }

                if (!hasOutput)
                {
                    log.Info(setName == DefaultSetName ? nameof(Resources.FUTD_NoBuildOutputDefined) : nameof(Resources.FUTD_NoBuildOutputDefinedInSet_1), setName);

                    return true;
                }

                Assumes.NotNull(earliestOutputPath);

                if (earliestOutputTime < state.LastItemsChangedAtUtc)
                {
                    log.Fail("ProjectItemsChangedSinceEarliestOutput", nameof(Resources.FUTD_SetOfItemsChangedMoreRecentlyThanOutput_3), state.LastItemsChangedAtUtc, earliestOutputPath, earliestOutputTime);

                    if (log.Level >= LogLevel.Info)
                    {
                        log.Indent++;

                        foreach ((bool isAdd, string itemType, UpToDateCheckInputItem item) in state.LastItemChanges.OrderBy(change => change.ItemType).ThenBy(change => change.Item.Path))
                        {
                            log.Info(isAdd ? nameof(Resources.FUTD_ChangedItemsAddition_4) : nameof(Resources.FUTD_ChangedItemsRemoval_4), itemType, item.Path, item.CopyType, item.TargetPath ?? "");
                        }

                        log.Indent--;
                    }

                    return false;
                }

                (string Path, DateTime? Time)? latestInput = null;

                foreach ((string input, string? itemType, bool isRequired) in inputs)
                {
                    token.ThrowIfCancellationRequested();

                    DateTime? inputTime = timestampCache.GetTimestampUtc(input);

                    if (inputTime == null)
                    {
                        if (isRequired)
                        {
                            return log.Fail("InputNotFound", itemType is null ? nameof(Resources.FUTD_RequiredInputNotFound_1) : nameof(Resources.FUTD_RequiredTypedInputNotFound_2), input, itemType ?? "");
                        }
                        else
                        {
                            log.Verbose(itemType is null ? nameof(Resources.FUTD_NonRequiredInputNotFound_1) : nameof(Resources.FUTD_NonRequiredTypedInputNotFound_2), input, itemType ?? "");
                        }
                    }

                    if (inputTime > earliestOutputTime)
                    {
                        return log.Fail("InputNewerThanEarliestOutput", itemType is null ? nameof(Resources.FUTD_InputNewerThanOutput_4) : nameof(Resources.FUTD_TypedInputNewerThanOutput_5), input, inputTime.Value, earliestOutputPath, earliestOutputTime, itemType ?? "");
                    }

                    if (inputTime > lastCheckedAtUtc && lastCheckedAtUtc != DateTime.MinValue)
                    {
                        // Bypass this test if no check has yet been performed. We handle that in CheckGlobalConditions.
                        return log.Fail("InputModifiedSinceLastCheck", itemType is null ? nameof(Resources.FUTD_InputModifiedSinceLastCheck_3) : nameof(Resources.FUTD_TypedInputModifiedSinceLastCheck_4), input, inputTime.Value, lastCheckedAtUtc, itemType ?? "");
                    }

                    if (latestInput is null || inputTime > latestInput.Value.Time)
                    {
                        latestInput = (input, inputTime);
                    }
                }

                if (log.Level >= LogLevel.Info)
                {
                    if (latestInput is null)
                    {
                        log.Info(setName == DefaultSetName ? nameof(Resources.FUTD_NoInputsDefined) : nameof(Resources.FUTD_NoInputsDefinedInSet_1), setName);
                    }
                    else
                    {
                        log.Info(setName == DefaultSetName ? nameof(Resources.FUTD_NoInputsNewerThanEarliestOutput_4) : nameof(Resources.FUTD_NoInputsNewerThanEarliestOutputInSet_5), earliestOutputPath, earliestOutputTime, latestInput.Value.Path, latestInput.Value.Time ?? (object)"null", setName);
                    }
                }

                return true;
            }

            IEnumerable<(string Path, string? ItemType, bool IsRequired)> CollectDefaultInputs()
            {
                if (state.MSBuildProjectFullPath != null)
                {
                    log.Verbose(nameof(Resources.FUTD_AddingProjectFileInputs));
                    log.Indent++;
                    log.VerboseLiteral(state.MSBuildProjectFullPath);
                    log.Indent--;
                    yield return (Path: state.MSBuildProjectFullPath, ItemType: null, IsRequired: true);
                }

                if (state.NewestImportInput != null)
                {
                    log.Verbose(nameof(Resources.FUTD_AddingNewestImportInput));
                    log.Indent++;
                    log.VerboseLiteral(state.NewestImportInput);
                    log.Indent--;
                    yield return (Path: state.NewestImportInput, ItemType: null, IsRequired: true);
                }

                foreach ((string itemType, ImmutableArray<UpToDateCheckInputItem> items) in state.InputSourceItemsByItemType)
                {
                    // Skip certain input item types (None, Content). These items do not contribute to build outputs,
                    // and so changes to them are not expected to produce updated outputs during build.
                    //
                    // These items may have CopyToOutputDirectory metadata, which is why we don't exclude them earlier.
                    // The need to schedule a build in order to copy files is handled separately.
                    if (!NonCompilationItemTypes.Contains(itemType))
                    {
                        log.Verbose(nameof(Resources.FUTD_AddingTypedInputs_1), itemType);
                        log.Indent++;

                        foreach (UpToDateCheckInputItem item in items)
                        {
                            string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(item.Path);
                            log.VerboseLiteral(absolutePath);
                            yield return (Path: absolutePath, itemType, IsRequired: true);
                        }

                        log.Indent--;
                    }
                }

                if (!state.ResolvedAnalyzerReferencePaths.IsEmpty)
                {
                    log.Verbose(nameof(Resources.FUTD_AddingTypedInputs_1), ResolvedAnalyzerReference.SchemaName);
                    log.Indent++;

                    foreach (string path in state.ResolvedAnalyzerReferencePaths)
                    {
                        string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                        log.VerboseLiteral(absolutePath);
                        yield return (Path: absolutePath, ItemType: ResolvedAnalyzerReference.SchemaName, IsRequired: true);
                    }

                    log.Indent--;
                }

                if (!state.ResolvedCompilationReferencePaths.IsEmpty)
                {
                    log.Verbose(nameof(Resources.FUTD_AddingTypedInputs_1), ResolvedCompilationReference.SchemaName);
                    log.Indent++;

                    foreach (string path in state.ResolvedCompilationReferencePaths)
                    {
                        System.Diagnostics.Debug.Assert(Path.IsPathRooted(path), "ResolvedCompilationReference path should be rooted");
                        log.VerboseLiteral(path);
                        yield return (Path: path, ItemType: ResolvedCompilationReference.SchemaName, IsRequired: true);
                    }

                    log.Indent--;
                }

                if (state.UpToDateCheckInputItemsByKindBySetName.TryGetValue(DefaultSetName, out ImmutableDictionary<string, ImmutableArray<string>>? upToDateCheckInputItems))
                {
                    log.Verbose(nameof(Resources.FUTD_AddingTypedInputs_1), UpToDateCheckInput.SchemaName);
                    log.Indent++;

                    foreach ((string kind, ImmutableArray<string> items) in upToDateCheckInputItems)
                    {
                        if (ShouldIgnoreItems(kind, items))
                        {
                            continue;
                        }

                        foreach (string path in items)
                        {
                            string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                            log.VerboseLiteral(absolutePath);
                            yield return (Path: absolutePath, ItemType: UpToDateCheckInput.SchemaName, IsRequired: true);
                        }
                    }

                    log.Indent--;
                }
            }

            IEnumerable<string> CollectDefaultOutputs()
            {
                if (state.UpToDateCheckOutputItemsByKindBySetName.TryGetValue(DefaultSetName, out ImmutableDictionary<string, ImmutableArray<string>>? upToDateCheckOutputItems))
                {
                    log.Verbose(nameof(Resources.FUTD_AddingTypedOutputs_1), UpToDateCheckOutput.SchemaName);
                    log.Indent++;

                    foreach ((string kind, ImmutableArray<string> items) in upToDateCheckOutputItems)
                    {
                        if (ShouldIgnoreItems(kind, items))
                        {
                            continue;
                        }

                        foreach (string path in items)
                        {
                            string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                            log.VerboseLiteral(absolutePath);
                            yield return absolutePath;
                        }
                    }

                    log.Indent--;
                }

                if (state.UpToDateCheckBuiltItemsByKindBySetName.TryGetValue(DefaultSetName, out ImmutableDictionary<string, ImmutableArray<string>>? upToDateCheckBuiltItems))
                {
                    log.Verbose(nameof(Resources.FUTD_AddingTypedOutputs_1), UpToDateCheckBuilt.SchemaName);
                    log.Indent++;

                    foreach ((string kind, ImmutableArray<string> items) in upToDateCheckBuiltItems)
                    {
                        if (ShouldIgnoreItems(kind, items))
                        {
                            continue;
                        }

                        foreach (string path in items)
                        {
                            string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                            log.VerboseLiteral(absolutePath);
                            yield return absolutePath;
                        }
                    }

                    log.Indent--;
                }
            }

            IEnumerable<(string Path, string? ItemType, bool IsRequired)> CollectSetInputs(string setName)
            {
                if (state.UpToDateCheckInputItemsByKindBySetName.TryGetValue(setName, out ImmutableDictionary<string, ImmutableArray<string>>? upToDateCheckInputItems))
                {
                    log.Verbose(nameof(Resources.FUTD_AddingTypedInputsInSet_2), UpToDateCheckInput.SchemaName, setName);
                    log.Indent++;

                    foreach ((string kind, ImmutableArray<string> items) in upToDateCheckInputItems)
                    {
                        if (ShouldIgnoreItems(kind, items))
                        {
                            continue;
                        }

                        foreach (string path in items)
                        {
                            string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                            log.VerboseLiteral(absolutePath);
                            yield return (Path: absolutePath, ItemType: UpToDateCheckInput.SchemaName, IsRequired: true);
                        }
                    }

                    log.Indent--;
                }
            }

            IEnumerable<string> CollectSetOutputs(string setName)
            {
                if (state.UpToDateCheckOutputItemsByKindBySetName.TryGetValue(setName, out ImmutableDictionary<string, ImmutableArray<string>>? upToDateCheckOutputItems))
                {
                    log.Verbose(nameof(Resources.FUTD_AddingTypedOutputsInSet_2), UpToDateCheckOutput.SchemaName, setName);
                    log.Indent++;

                    foreach ((string kind, ImmutableArray<string> items) in upToDateCheckOutputItems)
                    {
                        if (ShouldIgnoreItems(kind, items))
                        {
                            continue;
                        }

                        foreach (string path in items)
                        {
                            string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                            log.VerboseLiteral(absolutePath);
                            yield return absolutePath;
                        }
                    }

                    log.Indent--;
                }

                if (state.UpToDateCheckBuiltItemsByKindBySetName.TryGetValue(setName, out ImmutableDictionary<string, ImmutableArray<string>>? upToDateCheckBuiltItems))
                {
                    log.Verbose(nameof(Resources.FUTD_AddingTypedOutputsInSet_2), UpToDateCheckBuilt.SchemaName, setName);
                    log.Indent++;

                    foreach ((string kind, ImmutableArray<string> items) in upToDateCheckBuiltItems)
                    {
                        if (ShouldIgnoreItems(kind, items))
                        {
                            continue;
                        }

                        foreach (string path in items)
                        {
                            string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                            log.VerboseLiteral(absolutePath);
                            yield return absolutePath;
                        }
                    }

                    log.Indent--;
                }
            }

            bool ShouldIgnoreItems(string kind, ImmutableArray<string> items)
            {
                if (ignoreKinds?.Contains(kind) != true)
                {
                    return false;
                }

                if (log.Level >= LogLevel.Verbose)
                {
                    log.Indent++;
                   
                    foreach (string path in items)
                    {
                        string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                        log.Verbose(nameof(Resources.FUTD_SkippingIgnoredKindItem_2), absolutePath, kind);
                    }

                    log.Indent--;
                }

                return true;
            }
        }

        private bool CheckMarkers(Log log, in TimestampCache timestampCache, UpToDateCheckImplicitConfiguredInput state)
        {
            // Reference assembly copy markers are strange. The property is always going to be present on
            // references to SDK-based projects, regardless of whether or not those referenced projects
            // will actually produce a marker. And an item always will be present in an SDK-based project,
            // regardless of whether or not the project produces a marker. So, basically, we only check
            // here if the project actually produced a marker and we only check it against references that
            // actually produced a marker.

            if (Strings.IsNullOrWhiteSpace(state.CopyUpToDateMarkerItem) || state.CopyReferenceInputs.IsEmpty)
            {
                return true;
            }

            string markerFile = _configuredProject.UnconfiguredProject.MakeRooted(state.CopyUpToDateMarkerItem);

            if (log.Level >= LogLevel.Verbose)
            {
                log.Verbose(nameof(Resources.FUTD_AddingInputReferenceCopyMarkers));

                log.Indent++;

                foreach (string referenceMarkerFile in state.CopyReferenceInputs)
                {
                    log.VerboseLiteral(referenceMarkerFile);
                }

                log.Indent--;

                log.Verbose(nameof(Resources.FUTD_AddingOutputReferenceCopyMarker));
                log.Indent++;
                log.VerboseLiteral(markerFile);
                log.Indent--;
            }

            if (timestampCache.TryGetLatestInput(state.CopyReferenceInputs, out string? latestInputMarkerPath, out DateTime latestInputMarkerTime))
            {
                log.Info(nameof(Resources.FUTD_LatestWriteTimeOnInputMarker_2), latestInputMarkerTime, latestInputMarkerPath);
            }
            else
            {
                log.Info(nameof(Resources.FUTD_NoInputMarkersExist));
                return true;
            }

            DateTime? outputMarkerTime = timestampCache.GetTimestampUtc(markerFile);

            if (outputMarkerTime != null)
            {
                log.Info(nameof(Resources.FUTD_WriteTimeOnOutputMarker_2), outputMarkerTime, markerFile);
            }
            else
            {
                log.Info(nameof(Resources.FUTD_NoOutputMarkerExists_1), markerFile);
                return true;
            }

            if (outputMarkerTime < latestInputMarkerTime)
            {
                return log.Fail("InputMarkerNewerThanOutputMarker", nameof(Resources.FUTD_InputMarkerNewerThanOutputMarker));
            }

            return true;
        }

        private bool CheckCopiedOutputFiles(Log log, in TimestampCache timestampCache, UpToDateCheckImplicitConfiguredInput state, CancellationToken token)
        {
            foreach ((string destinationRelative, string sourceRelative) in state.CopiedOutputFiles)
            {
                token.ThrowIfCancellationRequested();

                string source = _configuredProject.UnconfiguredProject.MakeRooted(sourceRelative);
                string destination = _configuredProject.UnconfiguredProject.MakeRooted(destinationRelative);

                log.Info(nameof(Resources.FUTD_CheckingCopiedOutputFile), source);

                DateTime? sourceTime = timestampCache.GetTimestampUtc(source);

                if (sourceTime != null)
                {
                    log.Indent++;
                    log.Info(nameof(Resources.FUTD_SourceFileTimeAndPath_2), sourceTime, source);
                    log.Indent--;
                }
                else
                {
                    return log.Fail("CopySourceNotFound", nameof(Resources.FUTD_CheckingCopiedOutputFileSourceNotFound_2), source, destination);
                }

                DateTime? destinationTime = timestampCache.GetTimestampUtc(destination);

                if (destinationTime != null)
                {
                    log.Indent++;
                    log.Info(nameof(Resources.FUTD_DestinationFileTimeAndPath_2), destinationTime, destination);
                    log.Indent--;
                }
                else
                {
                    return log.Fail("CopyDestinationNotFound", nameof(Resources.FUTD_CheckingCopiedOutputFileDestinationNotFound_2), destination, source);
                }

                if (destinationTime < sourceTime)
                {
                    return log.Fail("CopySourceNewer", nameof(Resources.FUTD_CheckingCopiedOutputFileSourceNewer));
                }
            }

            return true;
        }

        private bool CheckCopyToOutputDirectoryFiles(Log log, in TimestampCache timestampCache, UpToDateCheckImplicitConfiguredInput state, CancellationToken token)
        {
            string outputFullPath = Path.Combine(state.MSBuildProjectDirectory, state.OutputRelativeOrFullPath);

            foreach ((_, ImmutableArray<UpToDateCheckInputItem> items) in state.InputSourceItemsByItemType)
            {
                foreach (UpToDateCheckInputItem item in items)
                {
                    // Only consider items with CopyType of CopyIfNewer (PreserveNewest)
                    if (item.CopyType != CopyType.CopyIfNewer)
                    {
                        continue;
                    }

                    token.ThrowIfCancellationRequested();

                    string rootedPath = _configuredProject.UnconfiguredProject.MakeRooted(item.Path);
                    string filename = Strings.IsNullOrEmpty(item.TargetPath) ? rootedPath : item.TargetPath;

                    if (string.IsNullOrEmpty(filename))
                    {
                        continue;
                    }

                    filename = _configuredProject.UnconfiguredProject.MakeRelative(filename);

                    log.Info(nameof(Resources.FUTD_CheckingPreserveNewestFile_1), rootedPath);

                    DateTime? itemTime = timestampCache.GetTimestampUtc(rootedPath);

                    if (itemTime != null)
                    {
                        log.Indent++;
                        log.Info(nameof(Resources.FUTD_SourceFileTimeAndPath_2), itemTime, rootedPath);
                        log.Indent--;
                    }
                    else
                    {
                        return log.Fail("CopyToOutputDirectorySourceNotFound", nameof(Resources.FUTD_CheckingPreserveNewestFileSourceNotFound_1), rootedPath);
                    }

                    string destination = Path.Combine(outputFullPath, filename);
                    DateTime? destinationTime = timestampCache.GetTimestampUtc(destination);

                    if (destinationTime != null)
                    {
                        log.Indent++;
                        log.Info(nameof(Resources.FUTD_DestinationFileTimeAndPath_2), destinationTime, destination);
                        log.Indent--;
                    }
                    else
                    {
                        return log.Fail("CopyToOutputDirectoryDestinationNotFound", nameof(Resources.FUTD_CheckingPreserveNewestFileDestinationNotFound_1), destination);
                    }

                    if (destinationTime < itemTime)
                    {
                        return log.Fail("CopyToOutputDirectorySourceNewer", nameof(Resources.FUTD_CheckingPreserveNewestSourceNewerThanDestination_2), rootedPath, destination);
                    }
                }
            }

            return true;
        }

        Task<bool> IBuildUpToDateCheckProvider.IsUpToDateAsync(BuildAction buildAction, TextWriter logWriter, CancellationToken cancellationToken)
        {
            return IsUpToDateAsync(buildAction, logWriter, ImmutableDictionary<string, string>.Empty, cancellationToken);
        }

        async Task<(bool IsUpToDate, string? FailureReason)> IBuildUpToDateCheckValidator.ValidateUpToDateAsync(CancellationToken cancellationToken)
        {
            bool isUpToDate = await IsUpToDateInternalAsync(TextWriter.Null, _lastGlobalProperties, isValidationRun: true, cancellationToken);

            string failureReason = isUpToDate ? "" : _lastFailureReason;

            return (isUpToDate, failureReason);
        }

        public Task<bool> IsUpToDateAsync(
            BuildAction buildAction,
            TextWriter logWriter,
            IImmutableDictionary<string, string> globalProperties,
            CancellationToken cancellationToken = default)
        {
            if (Volatile.Read(ref _isDisposed) != 0)
            {
                throw new ObjectDisposedException(nameof(BuildUpToDateCheck));
            }

            if (buildAction != BuildAction.Build)
            {
                return TaskResult.False;
            }

            return IsUpToDateInternalAsync(logWriter, globalProperties, isValidationRun: false, cancellationToken);
        }

        private async Task<bool> IsUpToDateInternalAsync(
            TextWriter logWriter,
            IImmutableDictionary<string, string> globalProperties,
            bool isValidationRun,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Cache the last-used set of global properties. We may be asked to validate this up-to-date check
            // once the build has completed (in ValidateUpToDateAsync), and will re-use the same set of global properties
            // to ensure parity.
            _lastGlobalProperties = globalProperties;

            // Start the stopwatch now, so we include any lock acquisition in the timing
            var sw = Stopwatch.StartNew();

            ISubscription subscription = Volatile.Read(ref _subscription);

            return await subscription.RunAsync(CheckAsync, updateLastCheckedAt: !isValidationRun, cancellationToken);

            async Task<bool> CheckAsync(UpToDateCheckConfiguredInput state, DateTime lastCheckedAtUtc, CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                // Short-lived cache of timestamp by path
                var timestampCache = new TimestampCache(_fileSystem);

                LogLevel requestedLogLevel = await _projectSystemOptions.GetFastUpToDateLoggingLevelAsync(token);
                var logger = new Log(logWriter, requestedLogLevel, sw, timestampCache, _configuredProject.UnconfiguredProject.FullPath ?? "", _telemetryService, state);

                try
                {
                    HashSet<string>? ignoreKinds = null;
                    if (globalProperties.TryGetValue(FastUpToDateCheckIgnoresKindsGlobalPropertyName, out string? ignoreKindsString))
                    {
                        ignoreKinds = new HashSet<string>(new LazyStringSplit(ignoreKindsString, ';'), StringComparer.OrdinalIgnoreCase);

                        if (requestedLogLevel >= LogLevel.Info && ignoreKinds.Count != 0)
                        {
                            logger.Info(nameof(Resources.FUTD_IgnoringKinds_1), ignoreKindsString);
                        }
                    }

                    bool logConfigurations = state.ImplicitInputs.Length > 1 && logger.Level >= LogLevel.Info;

                    foreach (UpToDateCheckImplicitConfiguredInput implicitState in state.ImplicitInputs)
                    {
                        if (logConfigurations)
                        {
                            // Only null when the FUTD check is disabled. If we get here, we are not disabled.
                            Assumes.NotNull(implicitState.ProjectConfiguration);

                            logger.Info(nameof(Resources.FUTD_CheckingConfiguration_1), implicitState.ProjectConfiguration.GetDisplayString());
                            logger.Indent++;
                        }

                        if (!CheckGlobalConditions(logger, lastCheckedAtUtc, validateFirstRun: !isValidationRun, implicitState) ||
                            !CheckInputsAndOutputs(logger, lastCheckedAtUtc, timestampCache, implicitState, ignoreKinds, token) ||
                            !CheckMarkers(logger, timestampCache, implicitState) ||
                            !CheckCopyToOutputDirectoryFiles(logger, timestampCache, implicitState, token) ||
                            !CheckCopiedOutputFiles(logger, timestampCache, implicitState, token))
                        {
                            return false;
                        }

                        if (logConfigurations)
                        {
                            logger.Indent--;
                        }
                    }

                    logger.UpToDate();
                    return true;
                }
                catch (Exception ex)
                {
                    return logger.Fail("Exception", nameof(Resources.FUTD_Exception_1), ex);
                }
                finally
                {
                    logger.Verbose(nameof(Resources.FUTD_Completed), sw.Elapsed.TotalMilliseconds);

                    _lastFailureReason = logger.FailureReason ?? "";
                }
            }
        }

        public Task<bool> IsUpToDateCheckEnabledAsync(CancellationToken cancellationToken = default)
        {
            return _projectSystemOptions.GetIsFastUpToDateCheckEnabledAsync(cancellationToken);
        }

        internal readonly struct TestAccessor
        {
            private readonly BuildUpToDateCheck _check;

            public TestAccessor(BuildUpToDateCheck check) => _check = check;

            public void SetSubscription(ISubscription subscription) => _check._subscription = subscription;
        }

        /// <summary>For unit testing only.</summary>
#pragma warning disable RS0043 // Do not call 'GetTestAccessor()'
        internal TestAccessor TestAccess => new(this);
#pragma warning restore RS0043
    }
}
