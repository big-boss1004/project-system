﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers
{
    [Export(DependencyRulesSubscriber.DependencyRulesSubscriberContract, typeof(IDependenciesRuleHandler))]
    [Export(typeof(IProjectDependenciesSubTreeProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal sealed partial class PackageRuleHandler : DependenciesRuleHandlerBase
    {
        public const string ProviderTypeString = "NuGetDependency";

        private static readonly SubTreeRootDependencyModel s_rootModel = new SubTreeRootDependencyModel(
            ProviderTypeString,
            Resources.PackagesNodeName,
            new DependencyIconSet(
                icon: ManagedImageMonikers.NuGetGrey,
                expandedIcon: ManagedImageMonikers.NuGetGrey,
                unresolvedIcon: ManagedImageMonikers.NuGetGreyWarning,
                unresolvedExpandedIcon: ManagedImageMonikers.NuGetGreyWarning),
            DependencyTreeFlags.NuGetSubTreeRootNode);

        private readonly ITargetFrameworkProvider _targetFrameworkProvider;

        [ImportingConstructor]
        public PackageRuleHandler(ITargetFrameworkProvider targetFrameworkProvider)
            : base(PackageReference.SchemaName, ResolvedPackageReference.SchemaName)
        {
            _targetFrameworkProvider = targetFrameworkProvider;
        }

        public override string ProviderType => ProviderTypeString;

        public override ImageMoniker ImplicitIcon => ManagedImageMonikers.NuGetGreyPrivate;

        public override void Handle(
            IImmutableDictionary<NamedIdentity, IComparable> versions,
            IImmutableDictionary<string, IProjectChangeDescription> changesByRuleName,
            RuleSource source,
            ITargetFramework targetFramework,
            CrossTargetDependenciesChangesBuilder changesBuilder)
        {
            IProjectChangeDescription evaluatedChanges = changesByRuleName[EvaluatedRuleName];

            HandleChangesForRule(
                resolved: false,
                projectChange: evaluatedChanges,
                isEvaluatedItemSpec: null);

            if (changesByRuleName.TryGetValue(ResolvedRuleName, out IProjectChangeDescription resolvedChanges))
            {
                HandleChangesForRule(
                    resolved: true,
                    projectChange: resolvedChanges,
                    isEvaluatedItemSpec: evaluatedChanges.After.Items.ContainsKey);
            }

            return;

            void HandleChangesForRule(bool resolved, IProjectChangeDescription projectChange, Func<string, bool>? isEvaluatedItemSpec)
            {
                if (projectChange.Difference.RemovedItems.Count != 0)
                {
                    foreach (string removedItem in projectChange.Difference.RemovedItems)
                    {
                        if (PackageDependencyMetadata.TryGetMetadata(
                            removedItem,
                            resolved,
                            properties: projectChange.Before.GetProjectItemProperties(removedItem)!,
                            isEvaluatedItemSpec,
                            targetFramework,
                            _targetFrameworkProvider,
                            out PackageDependencyMetadata metadata))
                        {
                            changesBuilder.Removed(targetFramework, ProviderTypeString, metadata.OriginalItemSpec);
                        }
                    }
                }

                if (projectChange.Difference.ChangedItems.Count != 0)
                {
                    foreach (string changedItem in projectChange.Difference.ChangedItems)
                    {
                        if (PackageDependencyMetadata.TryGetMetadata(
                            changedItem,
                            resolved,
                            properties: projectChange.After.GetProjectItemProperties(changedItem)!,
                            isEvaluatedItemSpec,
                            targetFramework,
                            _targetFrameworkProvider,
                            out PackageDependencyMetadata metadata))
                        {
                            changesBuilder.Removed(targetFramework, ProviderTypeString, metadata.OriginalItemSpec);
                            changesBuilder.Added(targetFramework, metadata.CreateDependencyModel());
                        }
                    }
                }

                if (projectChange.Difference.AddedItems.Count != 0)
                {
                    foreach (string addedItem in projectChange.Difference.AddedItems)
                    {
                        if (PackageDependencyMetadata.TryGetMetadata(
                            addedItem,
                            resolved,
                            properties: projectChange.After.GetProjectItemProperties(addedItem)!,
                            isEvaluatedItemSpec,
                            targetFramework,
                            _targetFrameworkProvider,
                            out PackageDependencyMetadata metadata))
                        {
                            changesBuilder.Added(targetFramework, metadata.CreateDependencyModel());
                        }
                    }
                }

                System.Diagnostics.Debug.Assert(evaluatedChanges.Difference.RenamedItems.Count == 0, "Project rule diff should not contain renamed items");
            }
        }

        public override IDependencyModel CreateRootDependencyNode() => s_rootModel;
    }
}
