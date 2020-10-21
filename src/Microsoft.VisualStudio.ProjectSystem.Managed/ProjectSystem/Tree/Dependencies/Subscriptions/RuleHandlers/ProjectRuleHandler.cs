﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.RuleHandlers
{
    [Export(DependencyRulesSubscriber.DependencyRulesSubscriberContract, typeof(IDependenciesRuleHandler))]
    [Export(typeof(IProjectDependenciesSubTreeProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal sealed class ProjectRuleHandler : DependenciesRuleHandlerBase
    {
        public const string ProviderTypeString = "Project";

        private static readonly DependencyGroupModel s_groupModel = new(
            ProviderTypeString,
            Resources.ProjectsNodeName,
            new DependencyIconSet(
                icon: KnownMonikers.Application,
                expandedIcon: KnownMonikers.Application,
                unresolvedIcon: KnownMonikers.ApplicationWarning,
                unresolvedExpandedIcon: KnownMonikers.ApplicationWarning),
            DependencyTreeFlags.ProjectDependencyGroup);

        public override string ProviderType => ProviderTypeString;

        public override ImageMoniker ImplicitIcon => KnownMonikers.ApplicationPrivate;

        [ImportingConstructor]
        public ProjectRuleHandler()
            : base(ProjectReference.SchemaName, ResolvedProjectReference.SchemaName)
        {
        }

        public override IDependencyModel CreateRootDependencyNode() => s_groupModel;

        protected override IDependencyModel CreateDependencyModel(
            string path,
            string originalItemSpec,
            bool resolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
        {
            return new ProjectDependencyModel(
                path,
                originalItemSpec,
                resolved,
                isImplicit,
                properties);
        }
    }
}
