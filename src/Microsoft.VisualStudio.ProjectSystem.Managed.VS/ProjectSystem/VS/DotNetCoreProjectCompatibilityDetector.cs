﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.References;
using Microsoft.VisualStudio.ProjectSystem.VS.UI;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    /// <see cref="IDotNetCoreProjectCompatibilityDetector"/>
    /// </summary>
    [Export(typeof(IDotNetCoreProjectCompatibilityDetector))]
    internal sealed class DotNetCoreProjectCompatibilityDetector : IDotNetCoreProjectCompatibilityDetector, IVsSolutionEvents, IVsSolutionLoadEvents, IDisposable
    {
        // The versions below this are compatible, versions above it are unsupported (stronger warning message and no don't show again option), equal
        // to are "partial" so tooling should generally work, new features may not have tooling.
        private static Version s_partialSupportedVersion = new Version(2, 1);

        private const string SupportedLearnMoreFwlink = "https://go.microsoft.com/fwlink/?linkid=866848";
        private const string UnsupportedLearnMoreFwlink = "https://go.microsoft.com/fwlink/?linkid=866796";
        private const string SuppressDotNewCoreWarningKey = @"ManagedProjectSystem\SuppressDotNewCoreWarning";
        private const string VersionCompatibilityFwlink = "https://go.microsoft.com/fwlink/?linkid=866798";
        private const string VersionDataFilename = "DotNetVersionCompatibility.json";
        private const int CacheFileValidHours = 24;

        [Guid("9B164E40-C3A2-4363-9BC5-EB4039DEF653")]
        private class SVsSettingsPersistenceManager { }

        enum CompatibilityLevel
        {
            Recommended = 0,
            Supported = 1,
            NotSupported = 2
        }
        [ImportingConstructor]
        public DotNetCoreProjectCompatibilityDetector([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, Lazy<IProjectServiceAccessor> projectAccessor,
                                                      Lazy<IDialogServices> dialogServices, Lazy<IProjectThreadingService> threadHandling, Lazy<IVsShellUtilitiesHelper> vsShellUtilitiesHelper,
                                                      Lazy<IFileSystem> fileSystem)
        {
            _serviceProvider = serviceProvider;
            ProjectServiceAccessor = projectAccessor;
            DialogServices = dialogServices;
            ThreadHandling = threadHandling;
            ShellUtilitiesHelper = vsShellUtilitiesHelper;
            FileSystem = fileSystem;
        }

        private readonly IServiceProvider _serviceProvider;
        private Lazy<IProjectServiceAccessor> ProjectServiceAccessor { get; set; }
        private Lazy<IDialogServices> DialogServices { get; set; }
        private Lazy<IProjectThreadingService> ThreadHandling { get; set; }
        private Lazy<IFileSystem> FileSystem { get; set; }
        private Lazy<IVsShellUtilitiesHelper> ShellUtilitiesHelper { get; set; }

        private string VersionDataCacheFile { get; set; }
        private Version OurVSVersion { get; set; }

        private uint _solutionCookie = VSConstants.VSCOOKIE_NIL;
        private bool _solutionOpened;
        private CompatibilityLevel _compatibilityLevelWarnedForThisSolution = CompatibilityLevel.Recommended;

        // Tracks how often we meed to look for new data
        private DateTime _timeCurVersionDataLastUpdatedUtc = DateTime.MinValue;
        private VersionCompatibilityData _curVersionCompatibilityData;

        public async Task InitializeAsync()
        {
            await ThreadHandling.Value.SwitchToUIThread();

            // Initialize our cache file
            string appDataFolder = await ShellUtilitiesHelper.Value.GetLocalAppDataFolderAsync(_serviceProvider).ConfigureAwait(true);
            if (appDataFolder != null)
            {
                VersionDataCacheFile = Path.Combine(appDataFolder, VersionDataFilename);
            }

            OurVSVersion = await ShellUtilitiesHelper.Value.GetVSVersionAsync(_serviceProvider).ConfigureAwait(true);

            IVsSolution solution = _serviceProvider.GetService<IVsSolution, SVsSolution>();
            Verify.HResult(solution.AdviseSolutionEvents(this, out _solutionCookie));

            // Check to see if a solution is already open. If so we set _solutionOpened to true so that subsequent projects added to 
            // this solution are processed.
            if (ErrorHandler.Succeeded(solution.GetProperty((int)__VSPROPID4.VSPROPID_IsSolutionFullyLoaded, out object isFullyLoaded)) && isFullyLoaded is bool && (bool)isFullyLoaded)
            {
                _solutionOpened = true;
            }
        }

        public void Dispose()
        {
            ThreadHandling.Value.VerifyOnUIThread();

            if (_solutionCookie != VSConstants.VSCOOKIE_NIL)
            {
                var solution = _serviceProvider.GetService<IVsSolution, SVsSolution>();
                if (solution != null)
                {
                    Verify.HResult(solution.UnadviseSolutionEvents(_solutionCookie));
                    _solutionCookie = VSConstants.VSCOOKIE_NIL;
                }
            }
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            // Only check this project if the solution is opened and we haven't already warned at the maximum level. Note that fAdded
            // is true for both add and a reload of an unloaded project
            if (_solutionOpened && fAdded == 1 && _compatibilityLevelWarnedForThisSolution != CompatibilityLevel.NotSupported)
            {
                UnconfiguredProject project = pHierarchy.AsUnconfiguredProject();
                if (project != null)
                {
                    ThreadHandling.Value.JoinableTaskFactory.RunAsync(async () =>
                    {
                        // Run on the background
                        await TaskScheduler.Default;

                        VersionCompatibilityData compatData = await GetVersionCmpatibilityDataAsync().ConfigureAwait(false);

                        // We need to check if this project has been newly created. Our projects will implement IProjectCreationState -we can 
                        // skip any that don't
                        var projectCreationState = project.Services.ExportProvider.GetExportedValueOrDefault<IProjectCreationState>();
                        if (projectCreationState != null && !projectCreationState.WasNewlyCreated)
                        {
                            CompatibilityLevel compatLevel = await GetProjectCompatibilityAsync(project, compatData).ConfigureAwait(false);
                            if (compatLevel != CompatibilityLevel.Recommended)
                            {
                                await WarnUserOfIncompatibleProjectAsync(compatLevel, compatData).ConfigureAwait(false);
                            }
                        }
                    });
                }
            }

            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            // Clear state flags
            _compatibilityLevelWarnedForThisSolution = CompatibilityLevel.Recommended;
            _solutionOpened = false;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Fired when the solution load process is fully complete, including all background loading 
        /// of projects. This event always fires after the initial opening of a solution 
        /// </summary>
        public int OnAfterBackgroundSolutionLoadComplete()
        {
            // Schedule this to run on idle
            ThreadHandling.Value.JoinableTaskFactory.RunAsync(async () =>
            {
                // Run on the background
                await TaskScheduler.Default;

                VersionCompatibilityData compatDataToUse = await GetVersionCmpatibilityDataAsync().ConfigureAwait(false);

                CompatibilityLevel finalCompatLevel = CompatibilityLevel.Recommended;
                IProjectService projectService = ProjectServiceAccessor.Value.GetProjectService();
                IEnumerable<UnconfiguredProject> projects = projectService.LoadedUnconfiguredProjects;
                foreach (var project in projects)
                {
                    // Track the most severe compatibility level
                    CompatibilityLevel compatLevel = await GetProjectCompatibilityAsync(project, compatDataToUse).ConfigureAwait(false);
                    if (compatLevel != CompatibilityLevel.Recommended && compatLevel > finalCompatLevel)
                    {
                        finalCompatLevel = compatLevel;
                    }
                }

                if (finalCompatLevel != CompatibilityLevel.Recommended)
                {

                    // Warn the user.
                    await WarnUserOfIncompatibleProjectAsync(finalCompatLevel, compatDataToUse).ConfigureAwait(false);
                }

                // Used so we know when to process newly added projects
                _solutionOpened = true;
            });

            return VSConstants.S_OK;
        }

        private async Task WarnUserOfIncompatibleProjectAsync(CompatibilityLevel compatLevel, VersionCompatibilityData compatData)
        {
            // Warn the user.
            await ThreadHandling.Value.SwitchToUIThread();

            // Check if already warned - this could happen in the off chance two projects are added very quickly since the detection work is 
            // scheduled on idle.
            if (_compatibilityLevelWarnedForThisSolution < compatLevel)
            {
                // Only want to warn once per solution
                _compatibilityLevelWarnedForThisSolution = compatLevel;

                IVsUIShell uiShell = _serviceProvider.GetService<IVsUIShell, SVsUIShell>();
                uiShell.GetAppName(out string caption);

                if (compatLevel == CompatibilityLevel.Supported)
                {
                    // Get current dontShowAgain value
                    var settingsManager = (ISettingsManager)_serviceProvider.GetService(typeof(SVsSettingsPersistenceManager));
                    bool suppressPrompt = false;
                    if (settingsManager != null)
                    {
                        suppressPrompt = settingsManager.GetValueOrDefault(SuppressDotNewCoreWarningKey, defaultValue: false);
                    }

                    if (!suppressPrompt)
                    {
                        suppressPrompt = DialogServices.Value.DontShowAgainMessageBox(caption, compatData.OpenSupportedMessage, VSResources.DontShowAgain, false, VSResources.LearnMore, SupportedLearnMoreFwlink);
                        if (suppressPrompt && settingsManager != null)
                        {
                            await settingsManager.SetValueAsync(SuppressDotNewCoreWarningKey, suppressPrompt, isMachineLocal: true).ConfigureAwait(true);
                        }
                    }
                }
                else
                {
                    DialogServices.Value.DontShowAgainMessageBox(caption, compatData.OpenUnsupportedMessage, null, false, VSResources.LearnMore, UnsupportedLearnMoreFwlink);
                }
            }
        }

        private async Task<CompatibilityLevel> GetProjectCompatibilityAsync(UnconfiguredProject project, VersionCompatibilityData compatData)
        {
            if (project.Capabilities.AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharp))
            {
                IProjectProperties properties = project.Services.ActiveConfiguredProjectProvider.ActiveConfiguredProject.Services.ProjectPropertiesProvider.GetCommonProperties();
                string tfm = await properties.GetEvaluatedPropertyValueAsync("TargetFrameworkMoniker").ConfigureAwait(false);
                if (!string.IsNullOrEmpty(tfm))
                {
                    var fw = new FrameworkName(tfm);
                    if (fw.Identifier.Equals(".NETCoreApp", StringComparison.OrdinalIgnoreCase))
                    {
                        return GetCompatibilityLevelFromVersion(fw.Version, compatData);
                    }
                    else if (fw.Identifier.Equals(".NETFramework", StringComparison.OrdinalIgnoreCase))
                    {
                        // The interesting case here is Asp.Net Core on full framework
                        IImmutableSet<IUnresolvedPackageReference> pkgReferences = await project.Services.ActiveConfiguredProjectProvider.ActiveConfiguredProject.Services.PackageReferences.GetUnresolvedReferencesAsync().ConfigureAwait(false);

                        // Look through the package references
                        foreach (var pkgRef in pkgReferences)
                        {
                            if (string.Equals(pkgRef.EvaluatedInclude, "Microsoft.AspNetCore.All", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(pkgRef.EvaluatedInclude, "Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase))
                            {
                                string verString = await pkgRef.Metadata.GetEvaluatedPropertyValueAsync("Version").ConfigureAwait(false);
                                if (!string.IsNullOrWhiteSpace(verString))
                                {
                                    // This is a semantic version string. We only care about the non-semantic version part
                                    int index = verString.IndexOfAny(new char[] { '-', '+' });
                                    if (index != -1)
                                    {
                                        verString = verString.Substring(0, index);
                                    }

                                    if (Version.TryParse(verString, out Version aspnetVersion))
                                    {
                                        return GetCompatibilityLevelFromVersion(aspnetVersion, compatData);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return CompatibilityLevel.Recommended;
        }

        /// <summary>
        /// Compares the passed in version to the compatibility data to determine the compat level
        /// </summary>
        private CompatibilityLevel GetCompatibilityLevelFromVersion(Version version, VersionCompatibilityData compatData)
        {
            // Omly compare major, minor. The presence of build with change the comparison. ie: 2.0 != 2.0.0
            if (version.Build != -1)
            {
                version = new Version(version.Major, version.Minor);
            }

            if (compatData.SupportedVersion != null)
            {
                if (version < compatData.SupportedVersion)
                {
                    return CompatibilityLevel.Recommended;
                }
                else if (version == compatData.SupportedVersion || (compatData.UnsupportedVersion != null && version < compatData.UnsupportedVersion))
                {
                    return CompatibilityLevel.Supported;
                }

                return CompatibilityLevel.NotSupported;
            }

            // Only has an unsupported version
            if (compatData.UnsupportedVersion != null)
            {
                if (version < compatData.UnsupportedVersion)
                {
                    return CompatibilityLevel.Recommended;
                }

                return CompatibilityLevel.NotSupported;
            }

            // No restrictions
            return CompatibilityLevel.Recommended;
        }

        /// <summary>
        /// Pings the server to download version compatibility information and stores this in a cached file in the users app data. If the cached file is
        /// less than 24 hours old, it uses that data. Otherwise it downloads from the server. If the download fails it will use the previously cached
        ///  file, or if that file doesn't not exist, it uses the data baked into this class
        /// </summary>
        private async Task<VersionCompatibilityData> GetVersionCmpatibilityDataAsync()
        {
            try
            {
                // Do we meed to update our cached data?
                if (_curVersionCompatibilityData == null || _timeCurVersionDataLastUpdatedUtc.AddHours(CacheFileValidHours).IsLaterThan(DateTime.UtcNow))
                {
                    string downLoadedVersionData = null;

                    // First try the cache
                    Dictionary<Version, VersionCompatibilityData> versionCompatData = GetCompabilityDataFromCacheFile(checkTimeStamp: true);

                    if (versionCompatData == null)
                    {
                        // Try downloading it 
                        try
                        {
                            var httpClient = new DefaultHttpClient();
                            downLoadedVersionData = await httpClient.GetStringAsync(new Uri(VersionCompatibilityFwlink)).ConfigureAwait(false);
                            versionCompatData = VersionCompatibilityData.DeserializeVersionData(downLoadedVersionData);
                        }
                        catch
                        {
                            // If we have a cached file use it
                            versionCompatData = GetCompabilityDataFromCacheFile(checkTimeStamp: false);
                        }
                    }

                    if (versionCompatData != null)
                    {
                        // Cache the data locally
                        if (downLoadedVersionData != null)
                        {
                            FileSystem.Value.WriteAllText(VersionDataCacheFile, downLoadedVersionData);
                        }

                        // First try to match exatly on our VS version and if that fails, match on just major, minor
                        if (versionCompatData.TryGetValue(OurVSVersion, out VersionCompatibilityData compatData) || versionCompatData.TryGetValue(new Version(OurVSVersion.Major, OurVSVersion.Minor), out compatData))
                        {

                            // Now fix up missing data
                            if (string.IsNullOrEmpty(compatData.OpenSupportedMessage))
                            {
                                compatData.OpenSupportedMessage = VSResources.PartialSupportedDotNetCoreProject;
                            }

                            if (string.IsNullOrEmpty(compatData.OpenUnsupportedMessage))
                            {
                                compatData.OpenUnsupportedMessage = string.Format(VSResources.NotSupportedDotNetCoreProject, s_partialSupportedVersion.Major, s_partialSupportedVersion.Minor);
                            }
                            _curVersionCompatibilityData = compatData;
                        }
                    }
                }
            }
            catch
            {

            }

            if (_curVersionCompatibilityData == null)
            {
                // Something failed,  use the compatibility data we shipped with
                _curVersionCompatibilityData = new VersionCompatibilityData()
                {
                    SupportedVersion = s_partialSupportedVersion,
                    OpenSupportedMessage = VSResources.PartialSupportedDotNetCoreProject,
                    OpenUnsupportedMessage = string.Format(VSResources.NotSupportedDotNetCoreProject, s_partialSupportedVersion.Major, s_partialSupportedVersion.Minor)
                };
            }

            return _curVersionCompatibilityData;
        }

        /// <summary>
        /// If the cached file is less than CacheFileValidHours hours old, it reads and serializes that data.
        /// </summary>
        private Dictionary<Version, VersionCompatibilityData> GetCompabilityDataFromCacheFile(bool checkTimeStamp)
        {
            try
            {
                // If the cached file exists and is newer than the valid cache time, try to use it
                if (FileSystem.Value.FileExists(VersionDataCacheFile) && (!checkTimeStamp || FileSystem.Value.LastFileWriteTimeUtc(VersionDataCacheFile).AddHours(CacheFileValidHours).IsLaterThan(DateTime.UtcNow)))
                {
                    return VersionCompatibilityData.DeserializeVersionData(FileSystem.Value.ReadAllText(VersionDataCacheFile));
                }
            }
            catch
            {

            }
            return null;
        }

        #region Unused

        /// <summary>
        /// Unused IVsSolutionEvents
        /// </summary>
        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Unused IVsSolutionLoadEvents
        /// </summary>
        public int OnAfterLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeBackgroundSolutionLoadBegins()
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeOpenSolution(string pszSolutionFilename)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryBackgroundLoadProjectBatch(out bool pfShouldDelayLoadToNextIdle)
        {
            pfShouldDelayLoadToNextIdle = false;
            return VSConstants.S_OK;
        }

        #endregion
    }
}
