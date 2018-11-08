﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class PackageAnalyzerAssemblyDependencyModel : DependencyModel
    {
        private static readonly DependencyIconSet s_iconSet = new DependencyIconSet(
            icon: KnownMonikers.CodeInformation,
            expandedIcon: KnownMonikers.CodeInformation,
            unresolvedIcon: ManagedImageMonikers.CodeInformationWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.CodeInformationWarning);

        public override IImmutableList<string> DependencyIDs { get; }

        public override DependencyIconSet IconSet => s_iconSet;

        public override string Name { get; }

        public override int Priority => Resolved ? Dependency.PackageAssemblyNodePriority : Dependency.UnresolvedReferenceNodePriority;

        public override string ProviderType => PackageRuleHandler.ProviderTypeString;

        public PackageAnalyzerAssemblyDependencyModel(
            string path,
            string originalItemSpec,
            string name,
            bool resolved,
            IImmutableDictionary<string, string> properties,
            IEnumerable<string> dependenciesIDs)
            : base(
                path,
                originalItemSpec,
                flags: DependencyTreeFlags.NuGetSubTreeNodeFlags,
                resolved,
                isImplicit: false,
                properties: properties,
                isTopLevel: false)
        {
            Requires.NotNullOrEmpty(name, nameof(name));

            Name = name;
            Caption = name;

            if (dependenciesIDs != null)
            {
                DependencyIDs = ImmutableArray.CreateRange(dependenciesIDs);
            }
        }
    }
}
