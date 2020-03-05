﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers
{
    [Export(DependencyRulesSubscriber.DependencyRulesSubscriberContract, typeof(IDependenciesRuleHandler))]
    [Export(typeof(IProjectDependenciesSubTreeProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class AnalyzerRuleHandler : DependenciesRuleHandlerBase
    {
        public const string ProviderTypeString = "AnalyzerDependency";

        private static readonly DependencyGroupModel s_groupModel = new DependencyGroupModel(
            ProviderTypeString,
            Resources.AnalyzersNodeName,
            new DependencyIconSet(
                icon: KnownMonikers.CodeInformation,
                expandedIcon: KnownMonikers.CodeInformation,
                unresolvedIcon: ManagedImageMonikers.CodeInformationWarning,
                unresolvedExpandedIcon: ManagedImageMonikers.CodeInformationWarning),
            DependencyTreeFlags.AnalyzerDependencyGroup);

        public AnalyzerRuleHandler()
            : base(AnalyzerReference.SchemaName, ResolvedAnalyzerReference.SchemaName)
        {
        }

        public override string ProviderType => ProviderTypeString;

        public override ImageMoniker ImplicitIcon => ManagedImageMonikers.CodeInformationPrivate;

        protected override bool ResolvedItemRequiresEvaluatedItem => false;

        public override IDependencyModel CreateRootDependencyNode() => s_groupModel;

        protected override IDependencyModel CreateDependencyModel(
            string path,
            string originalItemSpec,
            bool resolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
        {
            return new AnalyzerDependencyModel(
                path,
                originalItemSpec,
                resolved,
                isImplicit,
                properties);
        }
    }
}
