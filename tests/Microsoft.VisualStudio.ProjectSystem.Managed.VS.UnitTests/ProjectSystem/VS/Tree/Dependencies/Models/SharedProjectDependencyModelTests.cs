﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.RuleHandlers;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public class SharedSharedProjectDependencyModelTests
    {
        [Fact]
        public void SharedResolved()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");

            var model = new SharedProjectDependencyModel(
                "c:\\myPath.dll",
                "myOriginalItemSpec",
                isResolved: true,
                isImplicit: false,
                properties: properties);

            Assert.Equal(ProjectRuleHandler.ProviderTypeString, model.ProviderType);
            Assert.Equal("c:\\myPath.dll", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myPath", model.Caption);
            Assert.Equal(ResolvedProjectReference.SchemaName, model.SchemaName);
            Assert.True(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(ProjectReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(KnownMonikers.SharedProject, model.Icon);
            Assert.Equal(KnownMonikers.SharedProject, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.SharedProjectWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.SharedProjectWarning, model.UnresolvedExpandedIcon);
            Assert.True(model.Flags.Contains(DependencyTreeFlags.SharedProjectDependency));
            Assert.False(model.Flags.Contains(DependencyTreeFlags.SupportsRuleProperties));
            Assert.Equal(
                DependencyTreeFlags.ProjectDependency +
                DependencyTreeFlags.SharedProjectDependency +
                DependencyTreeFlags.GenericResolvedDependencyFlags -
                DependencyTreeFlags.SupportsRuleProperties,
                model.Flags);
        }

        [Fact]
        public void Unresolved()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");

            var model = new SharedProjectDependencyModel(
                "c:\\myPath.dll",
                "myOriginalItemSpec",
                isResolved: false,
                isImplicit: false,
                properties: properties);

            Assert.Equal(ProjectRuleHandler.ProviderTypeString, model.ProviderType);
            Assert.Equal("c:\\myPath.dll", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myPath", model.Caption);
            Assert.Equal(ProjectReference.SchemaName, model.SchemaName);
            Assert.False(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(ProjectReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(KnownMonikers.SharedProject, model.Icon);
            Assert.Equal(KnownMonikers.SharedProject, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.SharedProjectWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.SharedProjectWarning, model.UnresolvedExpandedIcon);
            Assert.Equal(
                DependencyTreeFlags.ProjectDependency +
                DependencyTreeFlags.SharedProjectDependency +
                DependencyTreeFlags.GenericUnresolvedDependencyFlags -
                DependencyTreeFlags.SupportsRuleProperties,
                model.Flags);
        }

        [Fact]
        public void Implicit()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");

            var model = new SharedProjectDependencyModel(
                "c:\\myPath.dll",
                "myOriginalItemSpec",
                isResolved: true,
                isImplicit: true,
                properties: properties);

            Assert.Equal(ProjectRuleHandler.ProviderTypeString, model.ProviderType);
            Assert.Equal("c:\\myPath.dll", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myPath", model.Caption);
            Assert.Equal(ResolvedProjectReference.SchemaName, model.SchemaName);
            Assert.True(model.Resolved);
            Assert.True(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(ProjectReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(ManagedImageMonikers.SharedProjectPrivate, model.Icon);
            Assert.Equal(ManagedImageMonikers.SharedProjectPrivate, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.SharedProjectWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.SharedProjectWarning, model.UnresolvedExpandedIcon);
            Assert.Equal(
                DependencyTreeFlags.ProjectDependency +
                DependencyTreeFlags.SharedProjectDependency +
                DependencyTreeFlags.GenericResolvedDependencyFlags -
                DependencyTreeFlags.SupportsRuleProperties -
                DependencyTreeFlags.SupportsRemove,
                model.Flags);
        }
    }
}
