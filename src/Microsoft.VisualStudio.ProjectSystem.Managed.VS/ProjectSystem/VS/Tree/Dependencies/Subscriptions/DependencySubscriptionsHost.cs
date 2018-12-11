﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Build.Execution;
using Microsoft.VisualStudio.Build;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    [Export(DependencySubscriptionsHostContract, typeof(ICrossTargetSubscriptionsHost))]
    [Export(typeof(IDependenciesSnapshotProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal sealed class DependencySubscriptionsHost : OnceInitializedOnceDisposedAsync, ICrossTargetSubscriptionsHost, IDependenciesSnapshotProvider
    {
        public const string DependencySubscriptionsHostContract = "DependencySubscriptionsHostContract";

        private readonly TimeSpan _dependenciesUpdateThrottleInterval = TimeSpan.FromMilliseconds(250);

        private readonly SemaphoreSlim _gate = new SemaphoreSlim(initialCount: 1);
        private readonly object _snapshotLock = new object();
        private readonly object _subscribersLock = new object();
        private readonly object _linksLock = new object();
        private readonly List<IDisposable> _evaluationSubscriptionLinks = new List<IDisposable>();

        private readonly IAggregateDependenciesSnapshotProvider _aggregateSnapshotProvider;
        private readonly ITargetFrameworkProvider _targetFrameworkProvider;
        private readonly IUnconfiguredProjectCommonServices _commonServices;
        private readonly ITaskDelayScheduler _dependenciesUpdateScheduler;
        private readonly Lazy<IAggregateCrossTargetProjectContextProvider> _contextProvider;
        private readonly IProjectAsynchronousTasksService _tasksService;
        private readonly IActiveConfiguredProjectSubscriptionService _activeConfiguredProjectSubscriptionService;
        private readonly IActiveProjectConfigurationRefreshService _activeProjectConfigurationRefreshService;

        [ImportMany] private readonly OrderPrecedenceImportCollection<IDependencyCrossTargetSubscriber> _dependencySubscribers;
        [ImportMany] private readonly OrderPrecedenceImportCollection<IProjectDependenciesSubTreeProvider> _subTreeProviders;
        [ImportMany] private readonly OrderPrecedenceImportCollection<IDependenciesSnapshotFilter> _snapshotFilters;

        private ImmutableArray<IDependencyCrossTargetSubscriber> _subscribers;
        private DependenciesSnapshot _currentSnapshot;
        private int _isInitialized;

        /// <summary>
        /// Current AggregateCrossTargetProjectContext - accesses to this field must be done with a lock.
        /// Note that at any given time, we can have only a single non-disposed aggregate project context.
        /// </summary>
        private AggregateCrossTargetProjectContext _currentAggregateProjectContext;

        [ImportingConstructor]
        public DependencySubscriptionsHost(
            IUnconfiguredProjectCommonServices commonServices,
            Lazy<IAggregateCrossTargetProjectContextProvider> contextProvider,
            [Import(ExportContractNames.Scopes.UnconfiguredProject)] IProjectAsynchronousTasksService tasksService,
            IActiveConfiguredProjectSubscriptionService activeConfiguredProjectSubscriptionService,
            IActiveProjectConfigurationRefreshService activeProjectConfigurationRefreshService,
            ITargetFrameworkProvider targetFrameworkProvider,
            IAggregateDependenciesSnapshotProvider aggregateSnapshotProvider)
            : base(commonServices.ThreadingService.JoinableTaskContext)
        {
            Requires.NotNull(contextProvider, nameof(contextProvider));
            Requires.NotNull(tasksService, nameof(tasksService));
            Requires.NotNull(activeConfiguredProjectSubscriptionService, nameof(activeConfiguredProjectSubscriptionService));
            Requires.NotNull(activeProjectConfigurationRefreshService, nameof(activeProjectConfigurationRefreshService));
            Requires.NotNull(targetFrameworkProvider, nameof(targetFrameworkProvider));
            Requires.NotNull(aggregateSnapshotProvider, nameof(aggregateSnapshotProvider));

            _commonServices = commonServices;
            _contextProvider = contextProvider;
            _tasksService = tasksService;
            _activeConfiguredProjectSubscriptionService = activeConfiguredProjectSubscriptionService;
            _activeProjectConfigurationRefreshService = activeProjectConfigurationRefreshService;
            _targetFrameworkProvider = targetFrameworkProvider;
            _aggregateSnapshotProvider = aggregateSnapshotProvider;

            _currentSnapshot = DependenciesSnapshot.CreateEmpty(_commonServices.Project.FullPath);

            _dependencySubscribers = new OrderPrecedenceImportCollection<IDependencyCrossTargetSubscriber>(
                projectCapabilityCheckProvider: commonServices.Project);

            _snapshotFilters = new OrderPrecedenceImportCollection<IDependenciesSnapshotFilter>(
                ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesLast,
                projectCapabilityCheckProvider: commonServices.Project);

            _subTreeProviders = new OrderPrecedenceImportCollection<IProjectDependenciesSubTreeProvider>(
                ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesLast,
                projectCapabilityCheckProvider: commonServices.Project);

            _dependenciesUpdateScheduler = new TaskDelayScheduler(
                _dependenciesUpdateThrottleInterval,
                commonServices.ThreadingService,
                tasksService.UnloadCancellationToken);

            ProjectFilePath = commonServices.Project.FullPath;
        }

        #region IDependenciesSnapshotProvider

        public IDependenciesSnapshot CurrentSnapshot => _currentSnapshot;

        public string ProjectFilePath { get; }

        public event EventHandler<ProjectRenamedEventArgs> SnapshotRenamed;
        public event EventHandler<SnapshotChangedEventArgs> SnapshotChanged;
        public event EventHandler<SnapshotProviderUnloadingEventArgs> SnapshotProviderUnloading;

        #endregion

        private ImmutableArray<IDependencyCrossTargetSubscriber> Subscribers
        {
            get
            {
                if (_subscribers.IsDefault)
                {
                    lock (_subscribersLock)
                    {
                        if (_subscribers.IsDefault)
                        {
                            foreach (Lazy<IDependencyCrossTargetSubscriber, IOrderPrecedenceMetadataView> subscriber in _dependencySubscribers)
                            {
                                subscriber.Value.DependenciesChanged += OnSubscriberDependenciesChanged;
                            }

                            _subscribers = _dependencySubscribers.ToImmutableValueArray();
                        }
                    }
                }

                return _subscribers;
            }
        }

#pragma warning disable RS0030 // symbol ProjectAutoLoad is banned
        [ProjectAutoLoad(ProjectLoadCheckpoint.ProjectFactoryCompleted)]
#pragma warning restore RS0030 // symbol ProjectAutoLoad is banned
        [AppliesTo(ProjectCapability.DependenciesTree)]
        public Task OnProjectFactoryCompletedAsync()
        {
            return AddInitialSubscriptionsAsync();
        }

        /// <summary>
        /// Workaround for CPS bug 375276 which causes double entry on InitializeAsync and exception
        /// "InvalidOperationException: The value factory has called for the value on the same instance".
        /// https://dev.azure.com/devdiv/DevDiv/_workitems/edit/375276
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (Interlocked.Exchange(ref _isInitialized, 1) == 0)
            {
                await InitializeAsync();
            }
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            await UpdateProjectContextAndSubscriptionsAsync();

            _commonServices.Project.ProjectUnloading += OnUnconfiguredProjectUnloadingAsync;
            _commonServices.Project.ProjectRenamed += OnUnconfiguredProjectRenamedAsync;

            _aggregateSnapshotProvider.RegisterSnapshotProvider(this);

            foreach (Lazy<IProjectDependenciesSubTreeProvider, IOrderPrecedenceMetadataView> provider in _subTreeProviders)
            {
                provider.Value.DependenciesChanged += OnSubtreeProviderDependenciesChanged;
            }
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            _commonServices.Project.ProjectUnloading -= OnUnconfiguredProjectUnloadingAsync;
            _commonServices.Project.ProjectRenamed -= OnUnconfiguredProjectRenamedAsync;

            _dependenciesUpdateScheduler.Dispose();

            _gate.Dispose();

            if (initialized)
            {
                DisposeAndClearSubscriptions();
            }

            return Task.CompletedTask;
        }

        private Task OnUnconfiguredProjectUnloadingAsync(object sender, EventArgs args)
        {
            _commonServices.Project.ProjectUnloading -= OnUnconfiguredProjectUnloadingAsync;
            _commonServices.Project.ProjectRenamed -= OnUnconfiguredProjectRenamedAsync;

            SnapshotProviderUnloading?.Invoke(this, new SnapshotProviderUnloadingEventArgs(this));

            foreach (Lazy<IDependencyCrossTargetSubscriber, IOrderPrecedenceMetadataView> subscriber in _dependencySubscribers)
            {
                subscriber.Value.DependenciesChanged -= OnSubscriberDependenciesChanged;
            }

            foreach (Lazy<IProjectDependenciesSubTreeProvider, IOrderPrecedenceMetadataView> provider in _subTreeProviders)
            {
                provider.Value.DependenciesChanged -= OnSubtreeProviderDependenciesChanged;
            }

            return Task.CompletedTask;
        }

        private Task OnUnconfiguredProjectRenamedAsync(object sender, ProjectRenamedEventArgs e)
        {
            SnapshotRenamed?.Invoke(this, e);

            return Task.CompletedTask;
        }

        private void OnAggregateContextChanged(
            AggregateCrossTargetProjectContext oldContext,
            AggregateCrossTargetProjectContext newContext)
        {
            if (oldContext == null)
            {
                // all new rules will be sent to new context , we don't need to clean up anything
                return;
            }

            var targetsToClean = new HashSet<ITargetFramework>();

            ImmutableArray<ITargetFramework> oldTargets = oldContext.TargetFrameworks;

            if (newContext == null)
            {
                targetsToClean.AddRange(oldTargets);
            }
            else
            {
                ImmutableArray<ITargetFramework> newTargets = newContext.TargetFrameworks;

                targetsToClean.AddRange(oldTargets.Except(newTargets));
            }

            if (targetsToClean.Count == 0)
            {
                return;
            }

            lock (_snapshotLock)
            {
                _currentSnapshot = _currentSnapshot.RemoveTargets(targetsToClean);
            }

            ScheduleDependenciesUpdate();
        }

        private void OnSubscriberDependenciesChanged(object sender, DependencySubscriptionChangedEventArgs e)
        {
            if (IsDisposing || IsDisposed)
            {
                return;
            }

            UpdateDependenciesSnapshot(e.Changes, e.Catalogs, e.ActiveTarget, CancellationToken.None);
        }

        private void OnSubtreeProviderDependenciesChanged(object sender, DependenciesChangedEventArgs e)
        {
            if (IsDisposing || IsDisposed || !e.Changes.AnyChanges())
            {
                return;
            }

            ITargetFramework targetFramework =
                string.IsNullOrEmpty(e.TargetShortOrFullName) || TargetFramework.Any.Equals(e.TargetShortOrFullName)
                    ? TargetFramework.Any
                    : _targetFrameworkProvider.GetTargetFramework(e.TargetShortOrFullName) ?? TargetFramework.Any;

            ImmutableDictionary<ITargetFramework, IDependenciesChanges> changes = ImmutableDictionary<ITargetFramework, IDependenciesChanges>.Empty.Add(targetFramework, e.Changes);

            UpdateDependenciesSnapshot(changes, catalogs: null, activeTargetFramework: null, e.Token);
        }

        private void UpdateDependenciesSnapshot(
            ImmutableDictionary<ITargetFramework, IDependenciesChanges> changes,
            IProjectCatalogSnapshot catalogs,
            ITargetFramework activeTargetFramework,
            CancellationToken token)
        {
            IImmutableSet<string> projectItemSpecs = GetProjectItemSpecsFromSnapshot();

            bool anyChanges = false;
            
            // Note: we are updating existing snapshot, not receiving a complete new one. Thus we must
            // ensure incremental updates are done in the correct order. This lock ensures that here.

            lock (_snapshotLock)
            {
                var updatedSnapshot = DependenciesSnapshot.FromChanges(
                    _commonServices.Project.FullPath,
                    _currentSnapshot,
                    changes,
                    catalogs,
                    activeTargetFramework,
                    _snapshotFilters.ToImmutableValueArray(),
                    _subTreeProviders.ToValueDictionary(p => p.ProviderType),
                    projectItemSpecs);

                if (!ReferenceEquals(_currentSnapshot, updatedSnapshot))
                {
                    _currentSnapshot = updatedSnapshot;
                    anyChanges = true;
                }
            }

            if (anyChanges)
            {
                // avoid unnecessary tree updates
                ScheduleDependenciesUpdate(token);
            }

            return;

            // Gets the set of items defined directly the project, and not included by imports.
            IImmutableSet<string> GetProjectItemSpecsFromSnapshot()
            {
                // We don't have catalog snapshot, we're likely updating because one of our project 
                // dependencies changed. Just return 'no data'
                if (catalogs == null)
                {
                    return null;
                }

                ImmutableHashSet<string>.Builder itemSpecs = ImmutableHashSet.CreateBuilder(StringComparer.OrdinalIgnoreCase);

                foreach (ProjectItemInstance item in catalogs.Project.ProjectInstance.Items)
                {
                    if (item.IsImported())
                    {
                        continue;
                    }

                    // Returns unescaped evaluated include
                    string itemSpec = item.EvaluatedInclude;
                    if (itemSpec.Length != 0)
                    {
                        itemSpecs.Add(itemSpec);
                    }
                }

                return itemSpecs.ToImmutable();
            }
        }

        private void ScheduleDependenciesUpdate(CancellationToken token = default)
        {
            _dependenciesUpdateScheduler.ScheduleAsyncTask(ct =>
            {
                if (ct.IsCancellationRequested || IsDisposing || IsDisposed)
                {
                    return Task.FromCanceled(ct);
                }

                IDependenciesSnapshot snapshot = _currentSnapshot;

                if (snapshot != null)
                {
                    SnapshotChanged?.Invoke(this, new SnapshotChangedEventArgs(snapshot, ct));
                }

                return Task.CompletedTask;
            }, token);
        }

        public async Task<AggregateCrossTargetProjectContext> GetCurrentAggregateProjectContext()
        {
            if (IsDisposing || IsDisposed)
            {
                return null;
            }

            await EnsureInitializedAsync();

            return await ExecuteWithinLockAsync(() => _currentAggregateProjectContext);
        }

        public Task<ConfiguredProject> GetConfiguredProject(ITargetFramework target)
        {
            return ExecuteWithinLockAsync(() => _currentAggregateProjectContext.GetInnerConfiguredProject(target));
        }

        private Task AddInitialSubscriptionsAsync()
        {
            JoinableTask joinableTask = _tasksService.LoadedProjectAsync(() =>
            {
                SubscribeToConfiguredProject(
                    _activeConfiguredProjectSubscriptionService,
                    e => OnProjectChangedAsync(e)); // evaluation

                foreach (IDependencyCrossTargetSubscriber subscriber in Subscribers)
                {
                    subscriber.InitializeSubscriber(this, _activeConfiguredProjectSubscriptionService);
                }

                return Task.CompletedTask;
            });

            return joinableTask.Task;
        }

        private async Task OnProjectChangedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
        {
            if (IsDisposing || IsDisposed)
            {
                return;
            }

            await EnsureInitializedAsync();

            await OnProjectChangedCoreAsync(e);
        }

        private Task OnProjectChangedCoreAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
        {
            // If "TargetFrameworks" property has changed, we need to refresh the project context and subscriptions.
            if (HasTargetFrameworksChanged(e))
            {
                return UpdateProjectContextAndSubscriptionsAsync();
            }

            return Task.CompletedTask;
        }

        private async Task UpdateProjectContextAndSubscriptionsAsync()
        {
            AggregateCrossTargetProjectContext previousProjectContext = await ExecuteWithinLockAsync(() => _currentAggregateProjectContext);

            AggregateCrossTargetProjectContext newProjectContext = await UpdateProjectContextAsync();

            if (previousProjectContext != newProjectContext)
            {
                // Dispose existing subscriptions.
                DisposeAndClearSubscriptions();

                // Add subscriptions for the configured projects in the new project context.
                await AddSubscriptionsAsync(newProjectContext);
            }
        }

        /// <summary>
        /// Ensures that <see cref="_currentAggregateProjectContext"/> is updated for the latest TargetFrameworks from the project properties
        /// and returns this value.
        /// </summary>
        private Task<AggregateCrossTargetProjectContext> UpdateProjectContextAsync()
        {
            // Ensure that only single thread is attempting to create a project context.
            return ExecuteWithinLockAsync(async () =>
            {
                AggregateCrossTargetProjectContext previousContext = _currentAggregateProjectContext;

                // Check if we have already computed the project context.
                if (previousContext != null)
                {
                    // For non-cross targeting projects, we can use the current project context if the TargetFramework hasn't changed.
                    // For cross-targeting projects, we need to verify that the current project context matches latest frameworks targeted by the project.
                    // If not, we create a new one and dispose the current one.
                    ConfigurationGeneral projectProperties = await _commonServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync();

                    if (!previousContext.IsCrossTargeting)
                    {
                        ITargetFramework newTargetFramework = _targetFrameworkProvider.GetTargetFramework((string)await projectProperties.TargetFramework.GetValueAsync());
                        if (previousContext.ActiveTargetFramework.Equals(newTargetFramework))
                        {
                            return previousContext;
                        }
                    }
                    else
                    {
                        // Check if the current project context is up-to-date for the current active and known project configurations.
                        ProjectConfiguration activeProjectConfiguration = _commonServices.ActiveConfiguredProject.ProjectConfiguration;
                        IImmutableSet<ProjectConfiguration> knownProjectConfigurations = await _commonServices.Project.Services.ProjectConfigurationsService.GetKnownProjectConfigurationsAsync();
                        if (knownProjectConfigurations.All(c => c.IsCrossTargeting()) &&
                            previousContext.HasMatchingTargetFrameworks(activeProjectConfiguration, knownProjectConfigurations))
                        {
                            return previousContext;
                        }
                    }
                }

                // Force refresh the CPS active project configuration (needs UI thread).
                await _commonServices.ThreadingService.SwitchToUIThread();
                await _activeProjectConfigurationRefreshService.RefreshActiveProjectConfigurationAsync();

                // Create new project context.
                _currentAggregateProjectContext = await _contextProvider.Value.CreateProjectContextAsync();

                OnAggregateContextChanged(previousContext, _currentAggregateProjectContext);

                return _currentAggregateProjectContext;
            });
        }

        private Task AddSubscriptionsAsync(AggregateCrossTargetProjectContext newProjectContext)
        {
            Requires.NotNull(newProjectContext, nameof(newProjectContext));

            JoinableTask joinableTask = _tasksService.LoadedProjectAsync(() =>
            {
                lock (_linksLock)
                {
                    foreach (ConfiguredProject configuredProject in newProjectContext.InnerConfiguredProjects)
                    {
                        SubscribeToConfiguredProject(
                            configuredProject.Services.ProjectSubscription,
                            e => OnProjectChangedCoreAsync(e)); // evaluation
                    }

                    foreach (IDependencyCrossTargetSubscriber subscriber in Subscribers)
                    {
                        subscriber.AddSubscriptions(newProjectContext);
                    }
                }

                return Task.CompletedTask;
            });

            return joinableTask.Task;
        }

        private void SubscribeToConfiguredProject(
            IProjectSubscriptionService subscriptionService,
            Func<IProjectVersionedValue<IProjectSubscriptionUpdate>, Task> action)
        {
            _evaluationSubscriptionLinks.Add(
                subscriptionService.ProjectRuleSource.SourceBlock.LinkToAsyncAction(
                    action,
                    ruleNames: ConfigurationGeneral.SchemaName));
        }

        private static bool HasTargetFrameworksChanged(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
        {
            // remember actual property value and compare
            return e.Value.ProjectChanges.TryGetValue(ConfigurationGeneral.SchemaName, out IProjectChangeDescription projectChange) &&
                   (projectChange.Difference.ChangedProperties.Contains(ConfigurationGeneral.TargetFrameworkProperty) ||
                    projectChange.Difference.ChangedProperties.Contains(ConfigurationGeneral.TargetFrameworksProperty));
        }

        private void DisposeAndClearSubscriptions()
        {
            lock (_linksLock)
            {
                foreach (IDependencyCrossTargetSubscriber subscriber in Subscribers)
                {
                    subscriber.ReleaseSubscriptions();
                }

                foreach (IDisposable link in _evaluationSubscriptionLinks)
                {
                    link.Dispose();
                }

                _evaluationSubscriptionLinks.Clear();
            }
        }

        private Task<T> ExecuteWithinLockAsync<T>(Func<Task<T>> task)
        {
            return _gate.ExecuteWithinLockAsync(JoinableCollection, JoinableFactory, task);
        }

        private Task<T> ExecuteWithinLockAsync<T>(Func<T> func)
        {
            return _gate.ExecuteWithinLockAsync(JoinableCollection, func);
        }
    }
}
