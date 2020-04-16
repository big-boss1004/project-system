﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    /// <summary>
    ///     Responsible for adding or removing the project from the startup list based on whether the project
    ///     is debuggable or not.
    /// </summary>
    internal class StartupProjectRegistrar : OnceInitializedOnceDisposedAsync
    {
        private readonly UnconfiguredProject _project;
        private readonly IVsService<IVsStartupProjectsListService> _startupProjectsListService;
        private readonly ISafeProjectGuidService _projectGuidService;
        private readonly IActiveConfiguredProjectSubscriptionService _projectSubscriptionService;
        private readonly ActiveConfiguredProject<DebuggerLaunchProviders> _launchProviders;

        private Guid _projectGuid;
        private IDisposable? _subscription;

        /// <remarks>
        /// <see cref="UnconfiguredProject"/> must be imported in the constructor in order for scope of this class' export to be correct.
        /// </remarks>
        [ImportingConstructor]
        public StartupProjectRegistrar(
            UnconfiguredProject project,
            IVsService<SVsStartupProjectsListService, IVsStartupProjectsListService> startupProjectsListService,
            IProjectThreadingService threadingService,
            ISafeProjectGuidService projectGuidService,
            IActiveConfiguredProjectSubscriptionService projectSubscriptionService,
            ActiveConfiguredProject<DebuggerLaunchProviders> launchProviders)
        : base(threadingService.JoinableTaskContext)
        {
            _project = project;
            _startupProjectsListService = startupProjectsListService;
            _projectGuidService = projectGuidService;
            _projectSubscriptionService = projectSubscriptionService;
            _launchProviders = launchProviders;
        }

        [ProjectAutoLoad(startAfter: ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        [AppliesTo(ProjectCapability.DotNet)]
        public Task InitializeAsync()
        {
            return InitializeAsync(CancellationToken.None);
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            _projectGuid = await _projectGuidService.GetProjectGuidAsync();

            Assumes.False(_projectGuid == Guid.Empty);

            _subscription = _projectSubscriptionService.ProjectRuleSource.SourceBlock.LinkToAsyncAction(
                target: OnProjectChangedAsync,
                _project);
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            if (initialized)
            {
                _subscription?.Dispose();
            }

            return Task.CompletedTask;
        }

        internal async Task OnProjectChangedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate>? _ = null)
        {
            bool isDebuggable = await _launchProviders.Value.IsDebuggableAsync();

            IVsStartupProjectsListService startupProjectsListService = await _startupProjectsListService.GetValueAsync();

            if (isDebuggable)
            {
                // If we're already registered, the service no-ops
                startupProjectsListService.AddProject(ref _projectGuid);
            }
            else
            {
                // If we're already unregistered, the service no-ops
                startupProjectsListService.RemoveProject(ref _projectGuid);
            }
        }

        [Export]
        internal class DebuggerLaunchProviders
        {
            [ImportingConstructor]
            public DebuggerLaunchProviders(ConfiguredProject project)
            {
                Debuggers = new OrderPrecedenceImportCollection<IDebugLaunchProvider>(projectCapabilityCheckProvider: project);
            }

            [ImportMany]
            public OrderPrecedenceImportCollection<IDebugLaunchProvider> Debuggers { get; }

            public async Task<bool> IsDebuggableAsync()
            {
                foreach (Lazy<IDebugLaunchProvider> provider in Debuggers)
                {
                    if (await provider.Value.CanLaunchAsync(DebugLaunchOptions.DesignTimeExpressionEvaluation))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
