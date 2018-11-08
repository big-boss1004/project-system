﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    [Export(DependencyRulesSubscriber.DependencyRulesSubscriberContract,
            typeof(ICrossTargetRuleHandler<DependenciesRuleChangeContext>))]
    [Export(typeof(IProjectDependenciesSubTreeProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class AssemblyRuleHandler : DependenciesRuleHandlerBase
    {
        public const string ProviderTypeString = "AssemblyDependency";

        private static readonly DependencyIconSet s_iconSet = new DependencyIconSet(
            icon: KnownMonikers.Reference,
            expandedIcon: KnownMonikers.Reference,
            unresolvedIcon: KnownMonikers.ReferenceWarning,
            unresolvedExpandedIcon: KnownMonikers.ReferenceWarning);

        private static readonly SubTreeRootDependencyModel s_rootModel = new SubTreeRootDependencyModel(
            ProviderTypeString,
            VSResources.AssembliesNodeName,
            s_iconSet,
            DependencyTreeFlags.AssemblySubTreeRootNodeFlags);

        public AssemblyRuleHandler()
            : base(AssemblyReference.SchemaName, ResolvedAssemblyReference.SchemaName)
        {
        }

        public override string ProviderType => ProviderTypeString;

        public override IDependencyModel CreateRootDependencyNode() => s_rootModel;

        protected override IDependencyModel CreateDependencyModel(
            string path,
            string originalItemSpec,
            bool resolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
        {
            return new AssemblyDependencyModel(
                path,
                originalItemSpec,
                resolved,
                isImplicit,
                properties);
        }

        public override ImageMoniker GetImplicitIcon()
        {
            return ManagedImageMonikers.ReferencePrivate;
        }
    }
}
