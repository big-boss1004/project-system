﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers;

#nullable enable

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    internal static class IDependencyExtensions
    {
        /// <summary>
        /// Specifies if there is unresolved child somewhere in the dependency graph
        /// </summary>
        public static bool HasUnresolvedDependency(this IDependency self, ITargetedDependenciesSnapshot snapshot)
        {
            return snapshot.CheckForUnresolvedDependencies(self);
        }

        /// <summary>
        /// Returns true if this reference itself is unresolved or it has at least 
        /// one unresolved reference somewhere in the dependency chain.
        /// </summary>
        public static bool IsOrHasUnresolvedDependency(this IDependency self, ITargetedDependenciesSnapshot snapshot)
        {
            return !self.Resolved || self.HasUnresolvedDependency(snapshot);
        }

        /// <summary>
        /// Returns a IDependencyViewModel for given dependency.
        /// </summary>
        public static IDependencyViewModel ToViewModel(this IDependency self, ITargetedDependenciesSnapshot snapshot)
        {
            return new DependencyViewModel(self, hasUnresolvedDependency: self.IsOrHasUnresolvedDependency(snapshot));
        }

        private sealed class DependencyViewModel : IDependencyViewModel
        {
            private readonly IDependency _model;
            private readonly bool _hasUnresolvedDependency;

            public DependencyViewModel(IDependency dependency, bool hasUnresolvedDependency)
            {
                _model = dependency;
                _hasUnresolvedDependency = hasUnresolvedDependency;
            }

            public IDependency? OriginalModel => _model;
            public string Caption => _model.Caption;
            public string? FilePath => _model.Id;
            public string? SchemaName => _model.SchemaName;
            public string? SchemaItemType => _model.SchemaItemType;
            public int Priority => _model.Priority;
            public ImageMoniker Icon => _hasUnresolvedDependency ? _model.IconSet.UnresolvedIcon : _model.IconSet.Icon;
            public ImageMoniker ExpandedIcon => _hasUnresolvedDependency ? _model.IconSet.UnresolvedExpandedIcon : _model.IconSet.ExpandedIcon;
            public IImmutableDictionary<string, string> Properties => _model.Properties;
            public ProjectTreeFlags Flags => _model.Flags;
        }


        /// <summary>
        /// Returns id having full path instead of OriginalItemSpec
        /// </summary>
        public static string GetTopLevelId(this IDependency self)
        {
            return string.IsNullOrEmpty(self.Path)
                ? self.Id
                : Dependency.GetID(self.TargetFramework, self.ProviderType, self.Path);
        }

        /// <summary>
        /// Returns id having full path instead of OriginalItemSpec
        /// </summary>
        public static bool TopLevelIdEquals(this IDependency self, string id)
        {
            return string.IsNullOrEmpty(self.Path)
                ? string.Equals(self.Id, id, StringComparison.OrdinalIgnoreCase)
                : Dependency.IdEquals(id, self.TargetFramework, self.ProviderType, self.Path);
        }

        /// <summary>
        /// Returns true if given dependency is a nuget package.
        /// </summary>
        public static bool IsPackage(this IDependency self)
        {
            return StringComparers.DependencyProviderTypes.Equals(self.ProviderType, PackageRuleHandler.ProviderTypeString);
        }

        /// <summary>
        /// Returns true if given dependency is a project.
        /// </summary>
        public static bool IsProject(this IDependency self)
        {
            return StringComparers.DependencyProviderTypes.Equals(self.ProviderType, ProjectRuleHandler.ProviderTypeString);
        }

        /// <summary>
        /// Returns true if given dependencies belong to the same targeted snapshot, i.e. have same target.
        /// </summary>
        public static bool HasSameTarget(this IDependency self, IDependency other)
        {
            Requires.NotNull(other, nameof(other));

            return self.TargetFramework.Equals(other.TargetFramework);
        }

        public static IDependency ToResolved(
            this IDependency dependency,
            string? schemaName = null,
            ImmutableArray<string> dependencyIDs = default)
        {
            return dependency.SetProperties(
                resolved: true,
                flags: dependency.GetResolvedFlags(),
                schemaName: schemaName,
                dependencyIDs: dependencyIDs);
        }

        public static IDependency ToUnresolved(
            this IDependency dependency,
            string? schemaName = null,
            ImmutableArray<string> dependencyIDs = default)
        {
            return dependency.SetProperties(
                resolved: false,
                flags: dependency.GetUnresolvedFlags(),
                schemaName: schemaName,
                dependencyIDs: dependencyIDs);
        }

        public static ProjectTreeFlags GetResolvedFlags(this IDependency dependency)
        {
            return dependency.Flags
                .Union(DependencyTreeFlags.ResolvedFlags)
                .Except(DependencyTreeFlags.UnresolvedFlags);
        }

        public static ProjectTreeFlags GetUnresolvedFlags(this IDependency dependency)
        {
            return dependency.Flags
                .Union(DependencyTreeFlags.UnresolvedFlags)
                .Except(DependencyTreeFlags.ResolvedFlags);
        }
    }
}
