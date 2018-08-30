﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    [Export(typeof(IDependencyCrossTargetSubscriber))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class DependencySharedProjectsSubscriber : OnceInitializedOnceDisposed, IDependencyCrossTargetSubscriber
    {
#pragma warning disable CA2213 // OnceInitializedOnceDisposedAsync are not tracked corretly by the IDisposeable analyzer
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(initialCount: 1);
#pragma warning restore CA2213
        private readonly List<IDisposable> _subscriptionLinks;
        private readonly IProjectAsynchronousTasksService _tasksService;
        private readonly IDependenciesSnapshotProvider _dependenciesSnapshotProvider;
        private ICrossTargetSubscriptionsHost _host;

        [ImportingConstructor]
        public DependencySharedProjectsSubscriber(
            IUnconfiguredProjectCommonServices commonServices,
            [Import(ExportContractNames.Scopes.UnconfiguredProject)]IProjectAsynchronousTasksService tasksService,
            IDependenciesSnapshotProvider dependenciesSnapshotProvider)
        {
            _tasksService = tasksService;
            _dependenciesSnapshotProvider = dependenciesSnapshotProvider;
            _subscriptionLinks = new List<IDisposable>();
        }

        public Task InitializeSubscriberAsync(ICrossTargetSubscriptionsHost host, IProjectSubscriptionService subscriptionService)
        {
            _host = host;

            SubscribeToConfiguredProject(subscriptionService);

            return Task.CompletedTask;
        }

        public Task AddSubscriptionsAsync(AggregateCrossTargetProjectContext newProjectContext)
        {
            Requires.NotNull(newProjectContext, nameof(newProjectContext));

            foreach (ConfiguredProject configuredProject in newProjectContext.InnerConfiguredProjects)
            {
                SubscribeToConfiguredProject(configuredProject.Services.ProjectSubscription);
            }

            return Task.CompletedTask;
        }

        public void ReleaseSubscriptions()
        {
            foreach (IDisposable link in _subscriptionLinks)
            {
                link.Dispose();
            }

            _subscriptionLinks.Clear();
        }

        public Task OnContextReleasedAsync(ITargetedProjectContext innerContext)
        {
            return Task.CompletedTask;
        }

        private void SubscribeToConfiguredProject(IProjectSubscriptionService subscriptionService)
        {
            var intermediateBlock =
                new BufferBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(
                    new ExecutionDataflowBlockOptions()
                    {
                        NameFormat = "Dependencies Shared Projects Input: {1}"
                    });

            _subscriptionLinks.Add(
                subscriptionService.ProjectRuleSource.SourceBlock.LinkTo(
                    intermediateBlock,
                    ruleNames: ConfigurationGeneral.SchemaName,
                    suppressVersionOnlyUpdates: false,
                    linkOptions: DataflowOption.PropagateCompletion));

            var actionBlock =
                new ActionBlock<IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectSharedFoldersSnapshot, IProjectCatalogSnapshot>>>
                    (e => OnProjectChangedAsync(e),
                    new ExecutionDataflowBlockOptions()
                    {
                        NameFormat = "Dependencies Shared Projects Input: {1}"
                    });

            _subscriptionLinks.Add(ProjectDataSources.SyncLinkTo(
                intermediateBlock.SyncLinkOptions(),
                subscriptionService.SharedFoldersSource.SourceBlock.SyncLinkOptions(),
                subscriptionService.ProjectCatalogSource.SourceBlock.SyncLinkOptions(),
                actionBlock,
                linkOptions: DataflowOption.PropagateCompletion));
        }

        private async Task OnProjectChangedAsync(
            IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectSharedFoldersSnapshot, IProjectCatalogSnapshot>> e)
        {
            if (IsDisposing || IsDisposed)
            {
                return;
            }

            EnsureInitialized();


            await _tasksService.LoadedProjectAsync(async () =>
            {
                if (_tasksService.UnloadCancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await HandleAsync(e).ConfigureAwait(false);
            });
        }

        private async Task HandleAsync(
            IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectSharedFoldersSnapshot, IProjectCatalogSnapshot>> e)
        {
            AggregateCrossTargetProjectContext currentAggregaceContext = await _host.GetCurrentAggregateProjectContext().ConfigureAwait(false);
            if (currentAggregaceContext == null)
            {
                return;
            }

            IProjectSubscriptionUpdate projectUpdate = e.Value.Item1;
            IProjectSharedFoldersSnapshot sharedProjectsUpdate = e.Value.Item2;
            IProjectCatalogSnapshot catalogs = e.Value.Item3;

            // We need to process the update within a lock to ensure that we do not release this context during processing.
            // TODO: Enable concurrent execution of updates themeselves, i.e. two separate invocations of HandleAsync
            //       should be able to run concurrently.
            using (await _gate.DisposableWaitAsync().ConfigureAwait(true))
            {
                // Get the inner workspace project context to update for this change.
                ITargetedProjectContext projectContextToUpdate = currentAggregaceContext
                    .GetInnerProjectContext(projectUpdate.ProjectConfiguration, out bool isActiveContext);
                if (projectContextToUpdate == null)
                {
                    return;
                }

                var dependencyChangeContext = new DependenciesRuleChangeContext(
                        currentAggregaceContext.ActiveProjectContext.TargetFramework, catalogs);

                ProcessSharedProjectsUpdates(sharedProjectsUpdate, projectContextToUpdate, dependencyChangeContext);

                if (dependencyChangeContext.AnyChanges)
                {
                    DependenciesChanged?.Invoke(this, new DependencySubscriptionChangedEventArgs(dependencyChangeContext));
                }
            }
        }

        private void ProcessSharedProjectsUpdates(
            IProjectSharedFoldersSnapshot sharedFolders,
            ITargetedProjectContext targetContext,
            DependenciesRuleChangeContext dependencyChangeContext)
        {
            Requires.NotNull(sharedFolders, nameof(sharedFolders));
            Requires.NotNull(targetContext, nameof(targetContext));
            Requires.NotNull(dependencyChangeContext, nameof(dependencyChangeContext));

            IDependenciesSnapshot snapshot = _dependenciesSnapshotProvider.CurrentSnapshot;
            if (!snapshot.Targets.TryGetValue(targetContext.TargetFramework, out ITargetedDependenciesSnapshot targetedSnapshot))
            {
                return;
            }

            IEnumerable<string> sharedFolderProjectPaths = sharedFolders.Value.Select(sf => sf.ProjectPath);
            var currentSharedImportNodes = targetedSnapshot.TopLevelDependencies
                .Where(x => x.Flags.Contains(DependencyTreeFlags.SharedProjectFlags))
                .ToList();
            IEnumerable<string> currentSharedImportNodePaths = currentSharedImportNodes.Select(x => x.Path);

            // process added nodes
            IEnumerable<string> addedSharedImportPaths = sharedFolderProjectPaths.Except(currentSharedImportNodePaths);
            foreach (string addedSharedImportPath in addedSharedImportPaths)
            {
                IDependencyModel added = CreateDependencyModel(addedSharedImportPath, targetContext.TargetFramework, resolved: true);
                dependencyChangeContext.IncludeAddedChange(targetContext.TargetFramework, added);
            }

            // process removed nodes
            IEnumerable<string> removedSharedImportPaths = currentSharedImportNodePaths.Except(sharedFolderProjectPaths);
            foreach (string removedSharedImportPath in removedSharedImportPaths)
            {
                IDependency existingImportNode = currentSharedImportNodes
                    .Where(node => PathHelper.IsSamePath(node.Path, removedSharedImportPath))
                    .FirstOrDefault();

                if (existingImportNode != null)
                {
                    IDependencyModel removed = CreateDependencyModel(removedSharedImportPath, targetContext.TargetFramework, resolved: true);
                    dependencyChangeContext.IncludeRemovedChange(targetContext.TargetFramework, removed);
                }
            }
        }

        private static IDependencyModel CreateDependencyModel(
                    string itemSpec,
                    ITargetFramework targetFramework,
                    bool resolved)
        {
            ImmutableDictionary<string, string> properties = ImmutableStringDictionary<string>.EmptyOrdinal;

            return new SharedProjectDependencyModel(
                ProjectRuleHandler.ProviderTypeString,
                itemSpec,
                itemSpec,
                DependencyTreeFlags.ProjectNodeFlags,
                resolved,
                false,
                properties);
        }

        public event EventHandler<DependencySubscriptionChangedEventArgs> DependenciesChanged;

        protected override void Initialize()
        {   
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ReleaseSubscriptions();
            }
        }
    }
}
