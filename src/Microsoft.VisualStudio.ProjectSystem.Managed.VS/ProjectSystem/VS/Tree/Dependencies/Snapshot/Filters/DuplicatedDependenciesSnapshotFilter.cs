﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters
{
    [Export(typeof(IDependenciesSnapshotFilter))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal class DuplicatedDependenciesSnapshotFilter : DependenciesSnapshotFilterBase
    {
        public const int Order = 101;

        public override IDependency BeforeAdd(
            string projectPath,
            ITargetFramework targetFramework,
            IDependency dependency, 
            ImmutableDictionary<string, IDependency>.Builder worldBuilder,
            ImmutableHashSet<IDependency>.Builder topLevelBuilder)
        {
            IDependency resultDependency = dependency;

            var matchingDependency = topLevelBuilder.FirstOrDefault(
                x => !x.Id.Equals(dependency.Id, StringComparison.OrdinalIgnoreCase)
                     && x.ProviderType.Equals(dependency.ProviderType, StringComparison.OrdinalIgnoreCase) 
                     && x.Caption.Equals(dependency.Caption, StringComparison.OrdinalIgnoreCase));
            var shouldApplyAlias = (matchingDependency == null)
                ? topLevelBuilder.Any(
                    x => !x.Id.Equals(dependency.Id)
                         && x.ProviderType.Equals(dependency.ProviderType, StringComparison.OrdinalIgnoreCase)
                         && x.Caption.Equals(
                             string.Format(CultureInfo.CurrentCulture, "{0} ({1})", dependency.Caption, x.OriginalItemSpec),
                             StringComparison.OrdinalIgnoreCase))
                : true;

            if (shouldApplyAlias)
            {
                if (matchingDependency != null)
                {
                    matchingDependency = matchingDependency.SetProperties(caption: matchingDependency.Alias);
                    topLevelBuilder.Add(matchingDependency);
                    worldBuilder[matchingDependency.Id] = matchingDependency;
                }

                resultDependency = resultDependency.SetProperties(caption: dependency.Alias);
            }

            return resultDependency;
        }
    }
}
