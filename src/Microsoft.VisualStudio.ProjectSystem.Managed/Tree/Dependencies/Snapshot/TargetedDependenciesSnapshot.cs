﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    internal sealed class TargetedDependenciesSnapshot
    {
        #region Factories and internal constructor

        public static TargetedDependenciesSnapshot CreateEmpty(string projectPath, ITargetFramework targetFramework, IProjectCatalogSnapshot? catalogs)
        {
            return new TargetedDependenciesSnapshot(
                projectPath,
                targetFramework,
                catalogs,
                ImmutableStringDictionary<IDependency>.EmptyOrdinalIgnoreCase);
        }

        /// <summary>
        /// Applies changes to <paramref name="previousSnapshot"/> and produces a new snapshot if required.
        /// If no changes are made, <paramref name="previousSnapshot"/> is returned unmodified.
        /// </summary>
        /// <returns>An updated snapshot, or <paramref name="previousSnapshot"/> if no changes occured.</returns>
        public static TargetedDependenciesSnapshot FromChanges(
            string projectPath,
            TargetedDependenciesSnapshot previousSnapshot,
            IDependenciesChanges changes,
            IProjectCatalogSnapshot? catalogs,
            ImmutableArray<IDependenciesSnapshotFilter> snapshotFilters,
            IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviderByProviderType,
            IImmutableSet<string>? projectItemSpecs)
        {
            Requires.NotNullOrWhiteSpace(projectPath, nameof(projectPath));
            Requires.NotNull(previousSnapshot, nameof(previousSnapshot));
            Requires.NotNull(changes, nameof(changes));
            Requires.Argument(!snapshotFilters.IsDefault, nameof(snapshotFilters), "Cannot be default.");
            Requires.NotNull(subTreeProviderByProviderType, nameof(subTreeProviderByProviderType));

            bool anyChanges = false;

            ITargetFramework targetFramework = previousSnapshot.TargetFramework;

            var worldBuilder = previousSnapshot.DependenciesWorld.ToBuilder();

            if (changes.RemovedNodes.Count != 0)
            {
                var context = new RemoveDependencyContext(worldBuilder);

                foreach (IDependencyModel removed in changes.RemovedNodes)
                {
                    Remove(context, removed);
                }
            }

            if (changes.AddedNodes.Count != 0)
            {
                var context = new AddDependencyContext(worldBuilder);

                foreach (IDependencyModel added in changes.AddedNodes)
                {
                    Add(context, added);
                }
            }

            // Also factor in any changes to path/framework/catalogs
            anyChanges =
                anyChanges ||
                !StringComparers.Paths.Equals(projectPath, previousSnapshot.ProjectPath) ||
                !targetFramework.Equals(previousSnapshot.TargetFramework) ||
                !Equals(catalogs, previousSnapshot.Catalogs);

            if (anyChanges)
            {
                return new TargetedDependenciesSnapshot(
                    projectPath,
                    targetFramework,
                    catalogs,
                    worldBuilder.ToImmutable());
            }

            return previousSnapshot;

            void Remove(RemoveDependencyContext context, IDependencyModel dependencyModel)
            {
                string dependencyId = Dependency.GetID(
                    targetFramework, dependencyModel.ProviderType, dependencyModel.Id);

                if (!context.TryGetDependency(dependencyId, out IDependency dependency))
                {
                    return;
                }

                context.Reset();

                foreach (IDependenciesSnapshotFilter filter in snapshotFilters)
                {
                    filter.BeforeRemove(
                        targetFramework,
                        dependency,
                        context);

                    anyChanges |= context.Changed;

                    if (!context.GetResult(filter))
                    {
                        // TODO breaking here denies later filters the opportunity to modify builders
                        return;
                    }
                }

                worldBuilder.Remove(dependencyId);
                anyChanges = true;
            }

            void Add(AddDependencyContext context, IDependencyModel dependencyModel)
            {
                // Create the unfiltered dependency
                IDependency? dependency = new Dependency(dependencyModel, targetFramework, projectPath);

                context.Reset();

                foreach (IDependenciesSnapshotFilter filter in snapshotFilters)
                {
                    filter.BeforeAddOrUpdate(
                        targetFramework,
                        dependency,
                        subTreeProviderByProviderType,
                        projectItemSpecs,
                        context);

                    dependency = context.GetResult(filter);

                    if (dependency == null)
                    {
                        break;
                    }
                }

                if (dependency != null)
                {
                    // A dependency was accepted
                    worldBuilder.Remove(dependency.Id);
                    worldBuilder.Add(dependency.Id, dependency);
                    anyChanges = true;
                }
                else
                {
                    // Even though the dependency was rejected, it's possible that filters made
                    // changes to other dependencies.
                    anyChanges |= context.Changed;
                }
            }
        }

        // Internal, for test use -- normal code should use the factory methods
        internal TargetedDependenciesSnapshot(
            string projectPath,
            ITargetFramework targetFramework,
            IProjectCatalogSnapshot? catalogs,
            ImmutableDictionary<string, IDependency> dependenciesWorld)
        {
            Requires.NotNullOrEmpty(projectPath, nameof(projectPath));
            Requires.NotNull(targetFramework, nameof(targetFramework));
            Requires.NotNull(dependenciesWorld, nameof(dependenciesWorld));
            Assumes.True(Equals(dependenciesWorld.KeyComparer, StringComparer.OrdinalIgnoreCase), $"{nameof(dependenciesWorld)} must have an {nameof(StringComparer.OrdinalIgnoreCase)} key comparer.");

            ProjectPath = projectPath;
            TargetFramework = targetFramework;
            Catalogs = catalogs;
            DependenciesWorld = dependenciesWorld;

            bool hasVisibleUnresolvedDependency = false;
            ImmutableArray<IDependency>.Builder topLevelDependencies = ImmutableArray.CreateBuilder<IDependency>();

            foreach ((string id, IDependency dependency) in dependenciesWorld)
            {
                System.Diagnostics.Debug.Assert(
                    string.Equals(id, dependency.Id),
                    "dependenciesWorld dictionary entry keys must match their value's ids.");

                if (!dependency.Resolved && dependency.Visible)
                {
                    hasVisibleUnresolvedDependency = true;
                }

                if (dependency.TopLevel)
                {
                    topLevelDependencies.Add(dependency);

                    if (!string.IsNullOrEmpty(dependency.Path))
                    {
                        _topLevelDependenciesByPathMap.Add(
                            Dependency.GetID(TargetFramework, dependency.ProviderType, dependency.Path),
                            dependency);
                    }
                }
            }

            HasVisibleUnresolvedDependency = hasVisibleUnresolvedDependency;
            TopLevelDependencies = topLevelDependencies.ToImmutable();
        }

        #endregion

        /// <summary>
        /// Path to project containing this snapshot.
        /// </summary>
        public string ProjectPath { get; }

        /// <summary>
        /// <see cref="ITargetFramework" /> for which project has dependencies contained in this snapshot.
        /// </summary>
        public ITargetFramework TargetFramework { get; }

        /// <summary>
        /// Catalogs of rules for project items (optional, custom dependency providers might not provide it).
        /// </summary>
        public IProjectCatalogSnapshot? Catalogs { get; }

        /// <summary>
        /// Top level project dependencies.
        /// </summary>
        public ImmutableArray<IDependency> TopLevelDependencies { get; }

        /// <summary>
        /// Contains all unique <see cref="IDependency"/> objects in the project, from all levels.
        /// Allows looking them up by their IDs.
        /// </summary>
        public ImmutableDictionary<string, IDependency> DependenciesWorld { get; }

        private readonly Dictionary<string, IDependency> _topLevelDependenciesByPathMap = new Dictionary<string, IDependency>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ImmutableArray<IDependency>> _dependenciesChildrenMap = new Dictionary<string, ImmutableArray<IDependency>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, bool> _unresolvedDescendantsMap = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Re-use an existing, private, object reference for locking, rather than allocating a dedicated object.</summary>
        private object SyncLock => _dependenciesChildrenMap;

        /// <summary>
        /// Specifies is this snapshot contains at least one unresolved/broken dependency at any level which is visible.
        /// </summary>
        public bool HasVisibleUnresolvedDependency { get; }

        /// <summary>
        /// Efficient API for checking if a given dependency has an unresolved child dependency at any level. 
        /// </summary>
        /// <param name="dependency"></param>
        /// <returns>Returns true if given dependency has unresolved child dependency at any level</returns>
        public bool CheckForUnresolvedDependencies(IDependency dependency)
        {
            lock (SyncLock)
            {
                if (!_unresolvedDescendantsMap.TryGetValue(dependency.Id, out bool unresolved))
                {
                    unresolved = _unresolvedDescendantsMap[dependency.Id] = FindUnresolvedDependenciesRecursive(dependency);
                }

                return unresolved;
            }

            bool FindUnresolvedDependenciesRecursive(IDependency parent)
            {
                if (parent.DependencyIDs.Length == 0)
                {
                    return false;
                }

                foreach (IDependency child in GetDependencyChildren(parent))
                {
                    if (!child.Visible)
                    {
                        return false;
                    }

                    if (!child.Resolved)
                    {
                        return true;
                    }

                    // If the dependency is already in the child map, it is resolved
                    // Checking here will prevent a stack overflow due to rechecking the same dependencies
                    if (_dependenciesChildrenMap.ContainsKey(child.Id))
                    {
                        return false;
                    }

                    if (!_unresolvedDescendantsMap.TryGetValue(child.Id, out bool depthFirstResult))
                    {
                        depthFirstResult = FindUnresolvedDependenciesRecursive(child);
                        _unresolvedDescendantsMap[parent.Id] = depthFirstResult;
                        return depthFirstResult;
                    }

                    if (depthFirstResult)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Efficient API for checking if a there is at least one unresolved dependency with given provider type.
        /// </summary>
        /// <param name="providerType">Provider type to check</param>
        /// <returns>Returns true if there is at least one unresolved dependency with given providerType.</returns>
        public bool CheckForUnresolvedDependencies(string providerType)
        {
            foreach ((string _, IDependency dependency) in DependenciesWorld)
            {
                if (StringComparers.DependencyProviderTypes.Equals(dependency.ProviderType, providerType) &&
                    dependency.Visible &&
                    !dependency.Resolved)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns a list of direct child nodes for given dependency
        /// </summary>
        /// <param name="dependency"></param>
        /// <returns></returns>
        public ImmutableArray<IDependency> GetDependencyChildren(IDependency dependency)
        {
            if (dependency.DependencyIDs.Length == 0)
            {
                return ImmutableArray<IDependency>.Empty;
            }

            lock (SyncLock)
            {
                if (!_dependenciesChildrenMap.TryGetValue(dependency.Id, out ImmutableArray<IDependency> children))
                {
                    children = _dependenciesChildrenMap[dependency.Id] = BuildChildren();
                }

                return children;
            }

            ImmutableArray<IDependency> BuildChildren()
            {
                ImmutableArray<IDependency>.Builder children =
                    ImmutableArray.CreateBuilder<IDependency>(dependency.DependencyIDs.Length);

                foreach (string id in dependency.DependencyIDs)
                {
                    if (DependenciesWorld.TryGetValue(id, out IDependency child) ||
                        _topLevelDependenciesByPathMap.TryGetValue(id, out child))
                    {
                        children.Add(child);
                    }
                }

                return children.Count == children.Capacity
                    ? children.MoveToImmutable()
                    : children.ToImmutable();
            }
        }

        public override string ToString() => $"{TargetFramework.FriendlyName} - {DependenciesWorld.Count} dependencies ({TopLevelDependencies.Length} top level) - {ProjectPath}";
    }
}
