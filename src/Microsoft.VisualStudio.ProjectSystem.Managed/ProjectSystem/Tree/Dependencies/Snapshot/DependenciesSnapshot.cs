﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot.Filters;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot
{
    /// <summary>
    /// Immutable snapshot of all top-level project dependencies across all target frameworks.
    /// </summary>
    internal sealed class DependenciesSnapshot
    {
        #region Factories and private constructor

        public static DependenciesSnapshot Empty { get; } = new DependenciesSnapshot(
            activeTargetFramework: TargetFramework.Empty,
            dependenciesByTargetFramework: ImmutableDictionary<ITargetFramework, TargetedDependenciesSnapshot>.Empty);

        /// <summary>
        /// Updates the <see cref="TargetedDependenciesSnapshot"/> corresponding to <paramref name="changedTargetFramework"/>,
        /// returning either:
        /// <list type="bullet">
        ///   <item>An updated <see cref="DependenciesSnapshot"/> object, or</item>
        ///   <item>the immutable <paramref name="previousSnapshot"/> if no changes were made.</item>
        /// </list>
        /// </summary>
        /// <remarks>
        /// As part of the update, each <see cref="IDependenciesSnapshotFilter"/> in <paramref name="snapshotFilters"/>
        /// is given a chance to influence the addition and removal of dependency data in the returned snapshot.
        /// </remarks>
        /// <returns>An updated snapshot, or <paramref name="previousSnapshot"/> if no changes occured.</returns>
        public static DependenciesSnapshot FromChanges(
            DependenciesSnapshot previousSnapshot,
            ITargetFramework changedTargetFramework,
            IDependenciesChanges? changes,
            IProjectCatalogSnapshot? catalogs,
            ImmutableArray<ITargetFramework> targetFrameworks,
            ITargetFramework? activeTargetFramework,
            ImmutableArray<IDependenciesSnapshotFilter> snapshotFilters,
            IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviderByProviderType,
            IImmutableSet<string>? projectItemSpecs)
        {
            Requires.NotNull(previousSnapshot, nameof(previousSnapshot));
            Requires.NotNull(changedTargetFramework, nameof(changedTargetFramework));
            Requires.Argument(!snapshotFilters.IsDefault, nameof(snapshotFilters), "Cannot be default.");
            Requires.NotNull(subTreeProviderByProviderType, nameof(subTreeProviderByProviderType));

            var builder = previousSnapshot.DependenciesByTargetFramework.ToBuilder();

            if (!builder.TryGetValue(changedTargetFramework, out TargetedDependenciesSnapshot previousTargetedSnapshot))
            {
                previousTargetedSnapshot = TargetedDependenciesSnapshot.CreateEmpty(changedTargetFramework, catalogs);
            }

            bool builderChanged = false;

            var newTargetedSnapshot = TargetedDependenciesSnapshot.FromChanges(
                previousTargetedSnapshot,
                changes,
                catalogs,
                snapshotFilters,
                subTreeProviderByProviderType,
                projectItemSpecs);

            if (!ReferenceEquals(previousTargetedSnapshot, newTargetedSnapshot))
            {
                builder[changedTargetFramework] = newTargetedSnapshot;
                builderChanged = true;
            }

            SyncTargetFrameworks();

            activeTargetFramework ??= previousSnapshot.ActiveTargetFramework;

            if (builderChanged)
            {
                // Dependencies-by-target-framework has changed
                return new DependenciesSnapshot(
                    activeTargetFramework,
                    builder.ToImmutable());
            }

            if (!activeTargetFramework.Equals(previousSnapshot.ActiveTargetFramework))
            {
                // The active target framework changed
                return new DependenciesSnapshot(
                    activeTargetFramework,
                    previousSnapshot.DependenciesByTargetFramework);
            }

            // Nothing has changed, so return the same snapshot
            return previousSnapshot;

            void SyncTargetFrameworks()
            {
                // Only sync if a the full list of target frameworks has been provided
                if (targetFrameworks.IsDefault)
                {
                    return;
                }

                // This is a long-winded way of doing this that minimises allocations

                // Ensure all required target frameworks are present
                foreach (ITargetFramework targetFramework in targetFrameworks)
                {
                    if (!builder.ContainsKey(targetFramework))
                    {
                        builder.Add(targetFramework, TargetedDependenciesSnapshot.CreateEmpty(targetFramework, catalogs));
                        builderChanged = true;
                    }
                }

                // Remove any extra target frameworks
                if (builder.Count != targetFrameworks.Length)
                {
                    // NOTE We need "ToList" here as "Except" is lazy, and attempts to remove from the builder
                    // while iterating will throw "Collection was modified"
                    IEnumerable<ITargetFramework> targetFrameworksToRemove = builder.Keys.Except(targetFrameworks).ToList();

                    foreach (ITargetFramework targetFramework in targetFrameworksToRemove)
                    {
                        builder.Remove(targetFramework);
                    }

                    builderChanged = true;
                }
            }
        }

        public DependenciesSnapshot SetTargets(
            ImmutableArray<ITargetFramework> targetFrameworks,
            ITargetFramework activeTargetFramework)
        {
            bool activeChanged = !activeTargetFramework.Equals(ActiveTargetFramework);

            ImmutableDictionary<ITargetFramework, TargetedDependenciesSnapshot> map = DependenciesByTargetFramework;

            var diff = new SetDiff<ITargetFramework>(map.Keys, targetFrameworks);

            map = map.RemoveRange(diff.Removed);
            map = map.AddRange(
                diff.Added.Select(
                    added => new KeyValuePair<ITargetFramework, TargetedDependenciesSnapshot>(
                        added,
                        TargetedDependenciesSnapshot.CreateEmpty(added, null))));

            if (activeChanged || !ReferenceEquals(map, DependenciesByTargetFramework))
            {
                return new DependenciesSnapshot(activeTargetFramework, map);
            }

            return this;
        }

        // Internal, for test use -- normal code should use the factory methods
        internal DependenciesSnapshot(
            ITargetFramework activeTargetFramework,
            ImmutableDictionary<ITargetFramework, TargetedDependenciesSnapshot> dependenciesByTargetFramework)
        {
            Requires.NotNull(activeTargetFramework, nameof(activeTargetFramework));
            Requires.NotNull(dependenciesByTargetFramework, nameof(dependenciesByTargetFramework));

            if (!activeTargetFramework.Equals(TargetFramework.Empty))
            {
                Requires.Argument(
                    dependenciesByTargetFramework.ContainsKey(activeTargetFramework),
                    nameof(dependenciesByTargetFramework),
                    $"Must contain {nameof(activeTargetFramework)} ({activeTargetFramework.FullName}).");
            }

            ActiveTargetFramework = activeTargetFramework;
            DependenciesByTargetFramework = dependenciesByTargetFramework;
        }

        #endregion

        /// <summary>
        /// Gets the active target framework for project.
        /// </summary>
        public ITargetFramework ActiveTargetFramework { get; }

        /// <summary>
        /// Gets a dictionary of dependencies by target framework.
        /// </summary>
        public ImmutableDictionary<ITargetFramework, TargetedDependenciesSnapshot> DependenciesByTargetFramework { get; }

        /// <summary>
        /// Gets whether this snapshot contains at least one visible unresolved dependency, for any target framework.
        /// </summary>
        public bool HasVisibleUnresolvedDependency => DependenciesByTargetFramework.Any(x => x.Value.HasVisibleUnresolvedDependency);

        /// <summary>
        /// Finds dependency for given id across all target frameworks.
        /// </summary>
        /// <param name="dependencyId">Unique id for dependency to be found.</param>
        /// <returns>The <see cref="IDependency"/> if found, otherwise <see langword="null"/>.</returns>
        public IDependency? FindDependency(string? dependencyId)
        {
            if (Strings.IsNullOrEmpty(dependencyId))
            {
                return null;
            }

            foreach ((ITargetFramework _, TargetedDependenciesSnapshot targetedDependencies) in DependenciesByTargetFramework)
            {
                IDependency? dependency = targetedDependencies.Dependencies.FirstOrDefault((dep, id) => dep.TopLevelIdEquals(id), dependencyId);

                if (dependency != null)
                {
                    return dependency;
                }
            }

            return null;
        }

        public override string ToString() => $"{DependenciesByTargetFramework.Count} target framework{(DependenciesByTargetFramework.Count == 1 ? "" : "s")}";
    }
}
