﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
                ImmutableArray<IDependency>.Empty);
        }

        /// <summary>
        /// Applies changes to <paramref name="previousSnapshot"/> and produces a new snapshot if required.
        /// If no changes are made, <paramref name="previousSnapshot"/> is returned unmodified.
        /// </summary>
        /// <returns>An updated snapshot, or <paramref name="previousSnapshot"/> if no changes occured.</returns>
        public static TargetedDependenciesSnapshot FromChanges(
            string projectPath,
            TargetedDependenciesSnapshot previousSnapshot,
            IDependenciesChanges? changes,
            IProjectCatalogSnapshot? catalogs,
            ImmutableArray<IDependenciesSnapshotFilter> snapshotFilters,
            IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviderByProviderType,
            IImmutableSet<string>? projectItemSpecs)
        {
            Requires.NotNullOrWhiteSpace(projectPath, nameof(projectPath));
            Requires.NotNull(previousSnapshot, nameof(previousSnapshot));
            Requires.Argument(!snapshotFilters.IsDefault, nameof(snapshotFilters), "Cannot be default.");
            Requires.NotNull(subTreeProviderByProviderType, nameof(subTreeProviderByProviderType));

            bool anyChanges = false;

            ITargetFramework targetFramework = previousSnapshot.TargetFramework;

            var dependencyById = previousSnapshot.Dependencies.ToDictionary(d => d.Id, StringComparers.DependencyTreeIds);

            if (changes != null && changes.RemovedNodes.Count != 0)
            {
                var context = new RemoveDependencyContext(dependencyById);

                foreach (IDependencyModel removed in changes.RemovedNodes)
                {
                    Remove(context, removed);
                }
            }

            if (changes != null && changes.AddedNodes.Count != 0)
            {
                var context = new AddDependencyContext(dependencyById);

                foreach (IDependencyModel added in changes.AddedNodes)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    // NOTE we still need to check this in case extensions (eg. WebTools) provide us with top level items that need to be filtered out
                    if (!added.TopLevel)
                        continue;
#pragma warning restore CS0618 // Type or member is obsolete

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
                    dependencyById.ToImmutableValueArray());
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

                dependencyById.Remove(dependencyId);
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
                    dependencyById.Remove(dependency.Id);
                    dependencyById.Add(dependency.Id, dependency);
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
            ImmutableArray<IDependency> dependencies)
        {
            Requires.NotNullOrEmpty(projectPath, nameof(projectPath));
            Requires.NotNull(targetFramework, nameof(targetFramework));
            Requires.Argument(!dependencies.IsDefault, nameof(dependencies), "Cannot be default.");

            ProjectPath = projectPath;
            TargetFramework = targetFramework;
            Catalogs = catalogs;
            Dependencies = dependencies;

            HasVisibleUnresolvedDependency = dependencies.Any(pair => pair.Visible && !pair.Resolved);
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
        /// Contains all <see cref="IDependency"/> objects in the project for the given target.
        /// </summary>
        public ImmutableArray<IDependency> Dependencies { get; }

        /// <summary>
        /// Gets whether this snapshot contains at least one visible unresolved dependency.
        /// </summary>
        public bool HasVisibleUnresolvedDependency { get; }

        /// <summary>
        /// Efficient API for checking if a there is at least one unresolved dependency with given provider type.
        /// </summary>
        /// <param name="providerType">Provider type to check</param>
        /// <returns>Returns true if there is at least one unresolved dependency with given providerType.</returns>
        public bool CheckForUnresolvedDependencies(string providerType)
        {
            if (HasVisibleUnresolvedDependency == false)
            {
                return false;
            }

            foreach (IDependency dependency in Dependencies)
            {
                if (!dependency.Resolved &&
                    dependency.Visible &&
                    StringComparers.DependencyProviderTypes.Equals(dependency.ProviderType, providerType))
                {
                    return true;
                }
            }

            return false;
        }

        public override string ToString() => $"{TargetFramework.FriendlyName} - {Dependencies.Length} dependencies - {ProjectPath}";
    }
}
