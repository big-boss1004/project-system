﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    [Export(DependencyRulesSubscriber.DependencyRulesSubscriberContract,
            typeof(ICrossTargetRuleHandler<DependenciesRuleChangeContext>))]
    [Export(typeof(IProjectDependenciesSubTreeProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class ProjectRuleHandler : DependenciesRuleHandlerBase
    {
        public const string ProviderTypeString = "ProjectDependency";

        private static readonly DependencyIconSet s_iconSet = new DependencyIconSet(
            icon: KnownMonikers.Application,
            expandedIcon: KnownMonikers.Application,
            unresolvedIcon: ManagedImageMonikers.ApplicationWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.ApplicationWarning);

        protected override string UnresolvedRuleName => ProjectReference.SchemaName;
        protected override string ResolvedRuleName => ResolvedProjectReference.SchemaName;
        public override string ProviderType => ProviderTypeString;

        [ImportingConstructor]
        public ProjectRuleHandler(IAggregateDependenciesSnapshotProvider aggregateSnapshotProvider,
                                  IDependenciesSnapshotProvider snapshotProvider,
                                  IUnconfiguredProjectCommonServices commonServices)
        {
            SnapshotProvider = snapshotProvider;

            aggregateSnapshotProvider.SnapshotChanged += OnAggregateSnapshotChanged;
            aggregateSnapshotProvider.SnapshotProviderUnloading += OnAggregateSnapshotProviderUnloading;

            // Unregister event handlers when the project unloads
            commonServices.Project.ProjectUnloading += OnUnconfiguredProjectUnloading;

            Task OnUnconfiguredProjectUnloading(object sender, EventArgs e)
            {
                commonServices.Project.ProjectUnloading -= OnUnconfiguredProjectUnloading;
                aggregateSnapshotProvider.SnapshotChanged -= OnAggregateSnapshotChanged;
                aggregateSnapshotProvider.SnapshotProviderUnloading -= OnAggregateSnapshotProviderUnloading;

                return Task.CompletedTask;
            }

            void OnAggregateSnapshotChanged(object sender, SnapshotChangedEventArgs e)
            {
                OnOtherProjectDependenciesChanged(e.Snapshot, shouldBeResolved: true);
            }

            void OnAggregateSnapshotProviderUnloading(object sender, SnapshotProviderUnloadingEventArgs e)
            {
                OnOtherProjectDependenciesChanged(e.SnapshotProvider.CurrentSnapshot, shouldBeResolved: false);
            }
        }

        private IDependenciesSnapshotProvider SnapshotProvider { get; }

        public override IDependencyModel CreateRootDependencyNode()
        {
            return new SubTreeRootDependencyModel(
                ProviderType,
                VSResources.ProjectsNodeName,
                s_iconSet,
                DependencyTreeFlags.ProjectSubTreeRootNodeFlags);
        }

        protected override IDependencyModel CreateDependencyModel(
            string providerType,
            string path,
            string originalItemSpec,
            bool resolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
        {
            return new ProjectDependencyModel(
                providerType,
                path,
                originalItemSpec,
                DependencyTreeFlags.ProjectNodeFlags,
                resolved,
                isImplicit,
                properties);
        }

        public override ImageMoniker GetImplicitIcon()
        {
            return ManagedImageMonikers.ApplicationPrivate;
        }

        /// <summary>
        /// When some other project's snapshot changed we need to check if our snapshot has a top level
        /// dependency on changed project. If it does we need to refresh those top level dependencies to 
        /// reflect changes.
        /// </summary>
        /// <param name="otherProjectSnapshot"></param>
        /// <param name="shouldBeResolved">
        /// Specifies if top-level project dependencies resolved status. When other project just had its dependencies
        /// changed, it is resolved=true (we check target's support when we add project dependencies). However when 
        /// other project is unloaded, we should mark top-level dependencies as unresolved.
        /// </param>
        private void OnOtherProjectDependenciesChanged(IDependenciesSnapshot otherProjectSnapshot, bool shouldBeResolved)
        {
            IDependenciesSnapshot projectSnapshot = SnapshotProvider.CurrentSnapshot;

            if (otherProjectSnapshot == null || projectSnapshot == null || projectSnapshot.Equals(otherProjectSnapshot))
            {
                // if any of the snapshots is not provided or this is the same project - skip
                return;
            }

            string otherProjectPath = otherProjectSnapshot.ProjectPath;

            var dependencyThatNeedChange = new List<IDependency>();
            foreach (ITargetedDependenciesSnapshot targetedDependencies in projectSnapshot.Targets.Values)
            {
                foreach (IDependency dependency in targetedDependencies.TopLevelDependencies)
                {
                    // We're only interested in project dependencies
                    if (!StringComparers.DependencyProviderTypes.Equals(dependency.ProviderType, ProviderTypeString))
                        continue;

                    if (!StringComparers.Paths.Equals(otherProjectPath, dependency.FullPath))
                        continue;

                    dependencyThatNeedChange.Add(dependency);
                    break;
                }
            }

            if (dependencyThatNeedChange.Count == 0)
            {
                // we don't have dependency on updated project
                return;
            }

            foreach (IDependency dependency in dependencyThatNeedChange)
            {
                IDependencyModel model = CreateDependencyModel(
                                ProviderType,
                                dependency.Path,
                                dependency.OriginalItemSpec,
                                shouldBeResolved,
                                dependency.Implicit,
                                dependency.Properties);

                var changes = new DependenciesChanges();

                // avoid unnecessary removing since, add would upgrade dependency in snapshot anyway,
                // but remove would require removing item from the tree instead of in-place upgrade.
                if (!shouldBeResolved)
                {
                    changes.IncludeRemovedChange(model);
                }

                changes.IncludeAddedChange(model);

                FireDependenciesChanged(
                    new DependenciesChangedEventArgs(
                        this,
                        dependency.TargetFramework.FullName,
                        changes,
                        catalogs: null,
                        dataSourceVersions: null));
            }
        }
    }
}
