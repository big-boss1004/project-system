﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.RuleHandlers;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models
{
    internal class AnalyzerDependencyModel : DependencyModel
    {
        private static readonly DependencyFlagCache s_flagCache = new(
            resolved: DependencyTreeFlags.AnalyzerDependency + DependencyTreeFlags.SupportsBrowse,
            unresolved: DependencyTreeFlags.AnalyzerDependency + DependencyTreeFlags.SupportsBrowse);

        private static readonly DependencyIconSet s_iconSet = new(
            icon: KnownMonikers.CodeInformation,
            expandedIcon: KnownMonikers.CodeInformation,
            unresolvedIcon: KnownMonikers.CodeInformationWarning,
            unresolvedExpandedIcon: KnownMonikers.CodeInformationWarning,
            implicitIcon: KnownMonikers.CodeInformationPrivate,
            implicitExpandedIcon: KnownMonikers.CodeInformationPrivate);

        public override DependencyIconSet IconSet => s_iconSet;

        public override string ProviderType => AnalyzerRuleHandler.ProviderTypeString;

        public override string? SchemaItemType => AnalyzerReference.PrimaryDataSourceItemType;

        public override string? SchemaName => Resolved ? ResolvedAnalyzerReference.SchemaName : AnalyzerReference.SchemaName;

        public AnalyzerDependencyModel(
            string path,
            string originalItemSpec,
            bool resolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
            : base(
                caption: resolved
                    ? System.IO.Path.GetFileNameWithoutExtension(path)
                    : path,
                path,
                originalItemSpec,
                flags: s_flagCache.Get(resolved, isImplicit),
                resolved,
                isImplicit,
                properties)
        {
        }
    }
}
