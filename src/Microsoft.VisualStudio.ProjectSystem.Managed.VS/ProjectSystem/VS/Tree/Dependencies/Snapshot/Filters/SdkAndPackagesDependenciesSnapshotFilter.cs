﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters
{
    /// <summary>
    /// Sdk nodes are actually packages and their hierarchy of dependencies is resolved from
    /// NuGet's assets json file. However Sdk themselves are brought by DesignTime build for rules
    /// SdkReference. This filter matches Sdk to their corresponding NuGet package and sets  
    /// of top level sdk dependencies from the package. Packages are invisible to avoid visual
    /// duplication and confusion.
    /// </summary>
    [Export(typeof(IDependenciesSnapshotFilter))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal class SdkAndPackagesDependenciesSnapshotFilter : DependenciesSnapshotFilterBase
    {
        public const int Order = 110;

        public override IDependency BeforeAdd(
            string projectPath,
            ITargetFramework targetFramework,
            IDependency dependency,
            ImmutableDictionary<string, IDependency>.Builder worldBuilder,
            ImmutableHashSet<IDependency>.Builder topLevelBuilder,
            Dictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviders,
            HashSet<string> projectItemSpecs,
            out bool filterAnyChanges)
        {
            filterAnyChanges = false;

            if (!dependency.TopLevel)
            {
                return dependency;
            }

            if (dependency.Flags.Contains(DependencyTreeFlags.SdkSubTreeNodeFlags))
            {
                // find package with the same name
                string packageModelId = dependency.Name;
                string packageId = Dependency.GetID(targetFramework, PackageRuleHandler.ProviderTypeString, packageModelId);

                if (worldBuilder.TryGetValue(packageId, out IDependency package) && package.Resolved)
                {
                    filterAnyChanges = true;
                    return dependency.ToResolved(
                        schemaName: ResolvedSdkReference.SchemaName,
                        dependencyIDs: package.DependencyIDs);
                }
            }
            else if (dependency.Flags.Contains(DependencyTreeFlags.PackageNodeFlags) && dependency.Resolved)
            {
                // find sdk with the same name
                string sdkModelId = dependency.Name;
                string sdkId = Dependency.GetID(targetFramework, SdkRuleHandler.ProviderTypeString, sdkModelId);

                if (worldBuilder.TryGetValue(sdkId, out IDependency sdk))
                {
                    filterAnyChanges = true;
                    sdk = sdk.ToResolved(
                        schemaName: ResolvedSdkReference.SchemaName,
                        dependencyIDs: dependency.DependencyIDs);

                    worldBuilder.Remove(sdk.Id);
                    worldBuilder.Add(sdk.Id, sdk);
                    topLevelBuilder.Remove(sdk);
                    topLevelBuilder.Add(sdk);
                }
            }

            return dependency;
        }

        public override IDependency BeforeRemove(
            string projectPath,
            ITargetFramework targetFramework,
            IDependency dependency,
            ImmutableDictionary<string, IDependency>.Builder worldBuilder,
            ImmutableHashSet<IDependency>.Builder topLevelBuilder,
            out bool filterAnyChanges)
        {
            filterAnyChanges = false;
            if (!dependency.TopLevel || !dependency.Resolved)
            {
                return dependency;
            }

            if (dependency.Flags.Contains(DependencyTreeFlags.PackageNodeFlags))
            {
                // find sdk with the same name and clean dependencyIDs
                string sdkModelId = dependency.Name;
                string sdkId = Dependency.GetID(targetFramework, SdkRuleHandler.ProviderTypeString, sdkModelId);

                if (worldBuilder.TryGetValue(sdkId, out IDependency sdk))
                {
                    filterAnyChanges = true;
                    // clean up sdk when corresponding package is removing
                    sdk = sdk.ToUnresolved(
                        schemaName: SdkReference.SchemaName,
                        dependencyIDs: ImmutableList<string>.Empty);

                    worldBuilder.Remove(sdk.Id);
                    worldBuilder.Add(sdk.Id, sdk);
                    topLevelBuilder.Remove(sdk);
                    topLevelBuilder.Add(sdk);
                }
            }

            return dependency;
        }
    }
}
