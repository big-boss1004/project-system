﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;

#nullable enable

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters
{
    /// <summary>
    /// Filter does not allow unresolved dependency rule to override resolved one in the snapshot. 
    /// When project changes and old resolved dependency cannot be resolved anymore, only removed
    /// resolved dependency rule can delete old dependency (not unresolved rule).
    /// </summary>
    [Export(typeof(IDependenciesSnapshotFilter))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal sealed class UnresolvedDependenciesSnapshotFilter : DependenciesSnapshotFilterBase
    {
        public const int Order = 100;

        public override void BeforeAddOrUpdate(
            ITargetFramework targetFramework,
            IDependency dependency,
            IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviderByProviderType,
            IImmutableSet<string>? projectItemSpecs,
            IAddDependencyContext context)
        {
            // TODO should this verify that the existing one is actually resolved?
            if (!dependency.Resolved && context.Contains(dependency.Id))
            {
                context.Reject();
                return;
            }

            context.Accept(dependency);
        }
    }
}
