﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.References;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [Export(typeof(IDependenciesTreeViewProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal class GroupedByTargetTreeViewProvider : TreeViewProviderBase
    {
        public const int Order = 1000;

        [ImportingConstructor]
        public GroupedByTargetTreeViewProvider(
            IDependenciesTreeServices treeServices,
            IDependenciesViewModelFactory viewModelFactory,
            IUnconfiguredProjectCommonServices commonServices)
            : base(commonServices.Project)
        {
            TreeServices = treeServices;
            ViewModelFactory = viewModelFactory;
            CommonServices = commonServices;
        }

        private IDependenciesTreeServices TreeServices { get; }
        private IDependenciesViewModelFactory ViewModelFactory { get; }
        private IUnconfiguredProjectCommonServices CommonServices { get; }

        /// <summary>
        /// Builds Dependencies tree for given dependencies snapshot
        /// </summary>
        public override async Task<IProjectTree> BuildTreeAsync(
            IProjectTree dependenciesTree,
            IDependenciesSnapshot snapshot,
            CancellationToken cancellationToken = default)
        {
            IProjectTree originalTree = dependenciesTree;
            var currentTopLevelNodes = new List<IProjectTree>();
            IProjectTree rememberNewNodes(IProjectTree rootNode, IEnumerable<IProjectTree> currentNodes)
            {
                if (currentNodes != null)
                {
                    currentTopLevelNodes.AddRange(currentNodes);
                }

                return rootNode;
            }

            if (snapshot.Targets.Where(x => !x.Key.Equals(TargetFramework.Any)).Count() == 1)
            {
                foreach (KeyValuePair<ITargetFramework, ITargetedDependenciesSnapshot> target in snapshot.Targets)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return originalTree;
                    }

                    dependenciesTree = await BuildSubTreesAsync(
                        dependenciesTree,
                        snapshot.ActiveTarget,
                        target.Value,
                        target.Value.Catalogs,
                        rememberNewNodes).ConfigureAwait(false);
                }
            }
            else
            {
                foreach (KeyValuePair<ITargetFramework, ITargetedDependenciesSnapshot> target in snapshot.Targets)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return originalTree;
                    }

                    if (target.Key.Equals(TargetFramework.Any))
                    {
                        dependenciesTree = await BuildSubTreesAsync(dependenciesTree,
                                                         snapshot.ActiveTarget,
                                                         target.Value,
                                                         target.Value.Catalogs,
                                                         rememberNewNodes).ConfigureAwait(false);
                    }
                    else
                    {
                        IProjectTree node = dependenciesTree.FindNodeByCaption(target.Key.FriendlyName);
                        bool shouldAddTargetNode = node == null;
                        IDependencyViewModel targetViewModel = ViewModelFactory.CreateTargetViewModel(target.Value);

                        node = CreateOrUpdateNode(node,
                                                  targetViewModel,
                                                  rule: null,
                                                  isProjectItem: false,
                                                  additionalFlags: ProjectTreeFlags.Create(ProjectTreeFlags.Common.BubbleUp));
                        node = await BuildSubTreesAsync(node, snapshot.ActiveTarget, target.Value, target.Value.Catalogs, CleanupOldNodes).ConfigureAwait(false);

                        if (shouldAddTargetNode)
                        {
                            dependenciesTree = dependenciesTree.Add(node).Parent;
                        }
                        else
                        {
                            dependenciesTree = node.Parent;
                        }

                        currentTopLevelNodes.Add(node);
                    }
                }
            }

            dependenciesTree = CleanupOldNodes(dependenciesTree, currentTopLevelNodes);

            // now update root Dependencies node status
            ProjectImageMoniker rootIcon = ViewModelFactory.GetDependenciesRootIcon(snapshot.HasUnresolvedDependency).ToProjectSystemType();
            return dependenciesTree.SetProperties(icon: rootIcon, expandedIcon: rootIcon);
        }

        public override IProjectTree FindByPath(IProjectTree root, string path)
        {
            if (root == null)
            {
                return null;
            }

            IProjectTree dependenciesNode = null;
            if (root.Flags.Contains(DependencyTreeFlags.DependenciesRootNodeFlags))
            {
                dependenciesNode = root;
            }
            else
            {
                dependenciesNode = root.GetSubTreeNode(DependencyTreeFlags.DependenciesRootNodeFlags);
            }

            if (dependenciesNode == null)
            {
                return null;
            }

            return FindByPathInternal(dependenciesNode, path);
        }

        private static IProjectTree FindByPathInternal(IProjectTree root, string path)
        {
            foreach (IProjectTree node in root.GetSelfAndDescendentsBreadthFirst())
            {
                if (string.Equals(node.FilePath, path, StringComparison.OrdinalIgnoreCase))
                    return node;
            }

            return null;
        }

        /// <summary>
        /// Builds all available sub trees under root: target framework or Dependencies node 
        /// when there is only one target.
        /// </summary>
        private async Task<IProjectTree> BuildSubTreesAsync(
            IProjectTree rootNode,
            ITargetFramework activeTarget,
            ITargetedDependenciesSnapshot targetedSnapshot,
            IProjectCatalogSnapshot catalogs,
            Func<IProjectTree, IEnumerable<IProjectTree>, IProjectTree> syncFunc)
        {
            var currentNodes = new List<IProjectTree>();
            var grouppedByProviderType = new Dictionary<string, List<IDependency>>(StringComparer.OrdinalIgnoreCase);
            foreach (IDependency dependency in targetedSnapshot.TopLevelDependencies)
            {
                if (!dependency.Visible)
                {
                    if (dependency.Flags.Contains(DependencyTreeFlags.ShowEmptyProviderRootNode))
                    {
                        // if provider sends special invisible node with flag ShowEmptyProviderRootNode, we 
                        // need to show provider node even if it does not have any dependencies.
                        grouppedByProviderType.Add(dependency.ProviderType, new List<IDependency>());
                    }

                    continue;
                }

                if (!grouppedByProviderType.TryGetValue(dependency.ProviderType, out List<IDependency> dependencies))
                {
                    dependencies = new List<IDependency>();
                    grouppedByProviderType.Add(dependency.ProviderType, dependencies);
                }

                dependencies.Add(dependency);
            }

            bool isActiveTarget = targetedSnapshot.TargetFramework.Equals(activeTarget);
            foreach (KeyValuePair<string, List<IDependency>> dependencyGroup in grouppedByProviderType)
            {
                IDependencyViewModel subTreeViewModel = ViewModelFactory.CreateRootViewModel(
                    dependencyGroup.Key, targetedSnapshot.CheckForUnresolvedDependencies(dependencyGroup.Key));
                IProjectTree subTreeNode = rootNode.FindNodeByCaption(subTreeViewModel.Caption);
                bool isNewSubTreeNode = subTreeNode == null;

                ProjectTreeFlags excludedFlags = ProjectTreeFlags.Empty;
                if (targetedSnapshot.TargetFramework.Equals(TargetFramework.Any))
                {
                    excludedFlags = ProjectTreeFlags.Create(ProjectTreeFlags.Common.BubbleUp);
                }

                subTreeNode = CreateOrUpdateNode(
                    subTreeNode,
                    subTreeViewModel,
                    rule: null,
                    isProjectItem: false,
                    excludedFlags: excludedFlags);

                subTreeNode = await BuildSubTreeAsync(
                    subTreeNode,
                    targetedSnapshot,
                    dependencyGroup.Value,
                    catalogs,
                    isActiveTarget,
                    shouldCleanup: !isNewSubTreeNode).ConfigureAwait(false);

                currentNodes.Add(subTreeNode);

                if (isNewSubTreeNode)
                {
                    rootNode = rootNode.Add(subTreeNode).Parent;
                }
                else
                {
                    rootNode = subTreeNode.Parent;
                }
            }

            return syncFunc(rootNode, currentNodes);
        }

        /// <summary>
        /// Builds a sub tree under root: target framework or Dependencies node when there is only one target.
        /// </summary>
        private async Task<IProjectTree> BuildSubTreeAsync(
            IProjectTree rootNode,
            ITargetedDependenciesSnapshot targetedSnapshot,
            IEnumerable<IDependency> dependencies,
            IProjectCatalogSnapshot catalogs,
            bool isActiveTarget,
            bool shouldCleanup)
        {
            var currentNodes = new List<IProjectTree>();
            foreach (IDependency dependency in dependencies)
            {
                IProjectTree dependencyNode = rootNode.FindNodeByCaption(dependency.Caption);
                bool isNewDependencyNode = dependencyNode == null;

                if (!isNewDependencyNode
                    && dependency.Flags.Contains(DependencyTreeFlags.SupportsHierarchy))
                {
                    if ((dependency.Resolved && dependencyNode.Flags.Contains(DependencyTreeFlags.UnresolvedFlags))
                        || (!dependency.Resolved && dependencyNode.Flags.Contains(DependencyTreeFlags.ResolvedFlags)))
                    {
                        // when transition from unresolved to resolved or vise versa - remove old node
                        // and re-add new  one to allow GraphProvider to recalculate children
                        isNewDependencyNode = true;
                        rootNode = dependencyNode.Remove();
                        dependencyNode = null;
                    }
                }

                dependencyNode = await CreateOrUpdateNodeAsync(dependencyNode, dependency, targetedSnapshot, catalogs, isActiveTarget).ConfigureAwait(false);
                currentNodes.Add(dependencyNode);

                if (isNewDependencyNode)
                {
                    rootNode = rootNode.Add(dependencyNode).Parent;
                }
                else
                {
                    rootNode = dependencyNode.Parent;
                }
            }

            if (shouldCleanup)
            {
                rootNode = CleanupOldNodes(rootNode, currentNodes);
            }

            return rootNode;
        }

        /// <summary>
        /// Removes nodes that don't exist anymore
        /// </summary>
        private static IProjectTree CleanupOldNodes(IProjectTree rootNode, IEnumerable<IProjectTree> currentNodes)
        {
            foreach (IProjectTree nodeToRemove in rootNode.Children.Except(currentNodes))
            {
                rootNode = rootNode.Remove(nodeToRemove);
            }

            return rootNode;
        }

        /// <summary>
        /// Updates or creates new node
        /// </summary>
        private async Task<IProjectTree> CreateOrUpdateNodeAsync(
            IProjectTree node,
            IDependency dependency,
            ITargetedDependenciesSnapshot targetedSnapshot,
            IProjectCatalogSnapshot catalogs,
            bool isProjectItem,
            ProjectTreeFlags? additionalFlags = null,
            ProjectTreeFlags? excludedFlags = null)
        {
            IRule rule = null;
            if (dependency.Flags.Contains(DependencyTreeFlags.SupportsRuleProperties))
            {
                rule = await TreeServices.GetRuleAsync(dependency, catalogs)
                                         .ConfigureAwait(false);
            }

            return CreateOrUpdateNode(
                node,
                dependency.ToViewModel(targetedSnapshot),
                rule,
                isProjectItem,
                additionalFlags,
                excludedFlags);
        }

        /// <summary>
        /// Updates or creates new node
        /// </summary>
        private IProjectTree CreateOrUpdateNode(
            IProjectTree node,
            IDependencyViewModel viewModel,
            IRule rule,
            bool isProjectItem,
            ProjectTreeFlags? additionalFlags = null,
            ProjectTreeFlags? excludedFlags = null)
        {
            return isProjectItem
                ? CreateOrUpdateProjectItemTreeNode(node, viewModel, rule, additionalFlags, excludedFlags)
                : CreateOrUpdateProjectTreeNode(node, viewModel, rule, additionalFlags, excludedFlags);
        }

        /// <summary>
        /// Updates or creates new IProjectTree node
        /// </summary>
        private IProjectTree CreateOrUpdateProjectTreeNode(
            IProjectTree node,
            IDependencyViewModel viewModel,
            IRule rule,
            ProjectTreeFlags? additionalFlags = null,
            ProjectTreeFlags? excludedFlags = null)
        {
            if (node == null)
            {
                // For IProjectTree remove ProjectTreeFlags.Common.Reference flag, otherwise CPS would fail to 
                // map this node to graph node and GraphProvider would be never called. 
                // Only IProjectItemTree can have this flag
                ProjectTreeFlags flags = FilterFlags(viewModel.Flags.Except(DependencyTreeFlags.BaseReferenceFlags),
                                        additionalFlags,
                                        excludedFlags);
                string filePath = (viewModel.OriginalModel != null && viewModel.OriginalModel.TopLevel && viewModel.OriginalModel.Resolved)
                                ? viewModel.OriginalModel.GetTopLevelId()
                                : viewModel.FilePath;

                node = TreeServices.CreateTree(
                    caption: viewModel.Caption,
                    filePath: filePath,
                    visible: true,
                    browseObjectProperties: rule,
                    flags: flags,
                    icon: viewModel.Icon.ToProjectSystemType(),
                    expandedIcon: viewModel.ExpandedIcon.ToProjectSystemType());
            }
            else
            {
                node = UpdateTreeNode(node, viewModel, rule);
            }

            return node;
        }

        /// <summary>
        /// Updates or creates new IProjectItemTree node
        /// </summary>
        private IProjectTree CreateOrUpdateProjectItemTreeNode(
            IProjectTree node,
            IDependencyViewModel viewModel,
            IRule rule,
            ProjectTreeFlags? additionalFlags = null,
            ProjectTreeFlags? excludedFlags = null)
        {
            if (node == null)
            {
                ProjectTreeFlags flags = FilterFlags(viewModel.Flags, additionalFlags, excludedFlags);
                string filePath = (viewModel.OriginalModel != null && viewModel.OriginalModel.TopLevel && viewModel.OriginalModel.Resolved)
                                    ? viewModel.OriginalModel.GetTopLevelId()
                                    : viewModel.FilePath;

                var itemContext = ProjectPropertiesContext.GetContext(
                    CommonServices.Project,
                    file: filePath,
                    itemType: viewModel.SchemaItemType,
                    itemName: filePath);

                node = TreeServices.CreateTree(
                    caption: viewModel.Caption,
                    itemContext: itemContext,
                    propertySheet: null,
                    visible: true,
                    browseObjectProperties: rule,
                    flags: viewModel.Flags,
                    icon: viewModel.Icon.ToProjectSystemType(),
                    expandedIcon: viewModel.ExpandedIcon.ToProjectSystemType());
            }
            else
            {
                node = UpdateTreeNode(node, viewModel, rule);
            }

            return node;
        }

        private IProjectTree UpdateTreeNode(
            IProjectTree node,
            IDependencyViewModel viewModel,
            IRule rule)
        {
            ProjectTreeCustomizablePropertyContext updatedNodeParentContext = GetCustomPropertyContext(node.Parent);
            var updatedValues = new ReferencesProjectTreeCustomizablePropertyValues
            {
                Caption = viewModel.Caption,
                Flags = viewModel.Flags,
                Icon = viewModel.Icon.ToProjectSystemType(),
                ExpandedIcon = viewModel.ExpandedIcon.ToProjectSystemType()
            };

            ApplyProjectTreePropertiesCustomization(updatedNodeParentContext, updatedValues);

            return node.SetProperties(
                    caption: viewModel.Caption,
                    browseObjectProperties: rule,
                    icon: viewModel.Icon.ToProjectSystemType(),
                    expandedIcon: viewModel.ExpandedIcon.ToProjectSystemType());
        }

        private static ProjectTreeFlags FilterFlags(
            ProjectTreeFlags flags,
            ProjectTreeFlags? additionalFlags,
            ProjectTreeFlags? excludedFlags)
        {
            if (additionalFlags != null && additionalFlags.HasValue)
            {
                flags = flags.Union(additionalFlags.Value);
            }

            if (excludedFlags != null && excludedFlags.HasValue)
            {
                flags = flags.Except(excludedFlags.Value);
            }

            return flags;
        }
    }
}
