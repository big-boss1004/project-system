﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.TreeView;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal class MockIDependenciesTreeServices : IDependenciesTreeServices
    {
        public IProjectTree CreateTree(
            string caption,
            IProjectPropertiesContext itemContext,
            IPropertySheet? propertySheet = null,
            IRule? browseObjectProperties = null,
            ProjectImageMoniker? icon = null,
            ProjectImageMoniker? expandedIcon = null,
            bool visible = true,
            ProjectTreeFlags? flags = null)
        {
            return new TestProjectTree
            {
                Caption = caption,
                FilePath = itemContext.File ?? caption,
                BrowseObjectProperties = browseObjectProperties,
                Icon = icon,
                ExpandedIcon = expandedIcon,
                Visible = visible,
                Flags = flags ?? ProjectTreeFlags.Empty,
                IsProjectItem = true
            };
        }

        public IProjectTree CreateTree(
            string caption,
            string? filePath,
            IRule? browseObjectProperties = null,
            ProjectImageMoniker? icon = null,
            ProjectImageMoniker? expandedIcon = null,
            bool visible = true,
            ProjectTreeFlags? flags = null)
        {
            return new TestProjectTree
            {
                Caption = caption,
                FilePath = filePath,
                BrowseObjectProperties = browseObjectProperties,
                Icon = icon,
                ExpandedIcon = expandedIcon,
                Visible = visible,
                Flags = flags ?? ProjectTreeFlags.Empty,
                IsProjectItem = false
            };
        }

        public Task<IRule?> GetBrowseObjectRuleAsync(IDependency dependency, IProjectCatalogSnapshot? catalogs)
        {
            var mockRule = new Mock<IRule>(MockBehavior.Strict);
            mockRule.Setup(x => x.Name).Returns(dependency.SchemaItemType);
            return Task.FromResult<IRule?>(mockRule.Object);
        }
    }
}
