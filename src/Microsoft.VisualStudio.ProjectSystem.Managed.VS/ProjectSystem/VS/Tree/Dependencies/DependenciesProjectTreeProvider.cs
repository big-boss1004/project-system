﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.References;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Provides the special "Dependencies" folder to project trees.
    /// </summary>
    [Export(ExportContractNames.ProjectTreeProviders.PhysicalViewRootGraft, typeof(IProjectTreeProvider))]
    [Export(typeof(IDependenciesTreeServices))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class DependenciesProjectTreeProvider :
        ProjectTreeProviderBase,
        IProjectTreeProvider,
        IDependenciesTreeServices
    {
        private readonly IDependencyTreeTelemetryService _treeTelemetryService;
        private readonly object _treeUpdateLock = new object();
        private Task _treeUpdateQueueTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="DependenciesProjectTreeProvider"/> class.
        /// </summary>
        [ImportingConstructor]
        public DependenciesProjectTreeProvider(
            IProjectThreadingService threadingService,
            UnconfiguredProject unconfiguredProject,
            IDependenciesSnapshotProvider dependenciesSnapshotProvider,
            [Import(DependencySubscriptionsHost.DependencySubscriptionsHostContract)] ICrossTargetSubscriptionsHost dependenciesHost,
            [Import(ExportContractNames.Scopes.UnconfiguredProject)] IProjectAsynchronousTasksService tasksService,
            IDependencyTreeTelemetryService treeTelemetryService)
            : base(threadingService, unconfiguredProject)
        {
            ProjectTreePropertiesProviders = new OrderPrecedenceImportCollection<IProjectTreePropertiesProvider>(
                ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesLast,
                projectCapabilityCheckProvider: unconfiguredProject);

            ViewProviders = new OrderPrecedenceImportCollection<IDependenciesTreeViewProvider>(
                ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst,
                projectCapabilityCheckProvider: unconfiguredProject);

            DependenciesSnapshotProvider = dependenciesSnapshotProvider;
            DependenciesHost = dependenciesHost;
            TasksService = tasksService;
            _treeTelemetryService = treeTelemetryService;

            // Hook this so we can unregister the snapshot change event when the project unloads
            unconfiguredProject.ProjectUnloading += OnUnconfiguredProjectUnloading;

            Task OnUnconfiguredProjectUnloading(object sender, EventArgs args)
            {
                UnconfiguredProject.ProjectUnloading -= OnUnconfiguredProjectUnloading;
                DependenciesSnapshotProvider.SnapshotChanged -= OnDependenciesSnapshotChanged;

                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Gets the collection of <see cref="IProjectTreePropertiesProvider"/> imports 
        /// that apply to the references tree.
        /// </summary>
        [ImportMany(ReferencesProjectTreeCustomizablePropertyValues.ContractName)]
        private OrderPrecedenceImportCollection<IProjectTreePropertiesProvider> ProjectTreePropertiesProviders { get; }

        [ImportMany]
        private OrderPrecedenceImportCollection<IDependenciesTreeViewProvider> ViewProviders { get; }

        private ICrossTargetSubscriptionsHost DependenciesHost { get; }

        private IDependenciesSnapshotProvider DependenciesSnapshotProvider { get; }

        private IProjectAsynchronousTasksService TasksService { get; }

        /// <summary>
        /// Keeps latest updated snapshot of all rules schema catalogs
        /// </summary>
        private IImmutableDictionary<string, IPropertyPagesCatalog> NamedCatalogs { get; set; }

        /// <summary>
        /// See <see cref="IProjectTreeProvider"/>
        /// </summary>
        /// <remarks>
        /// This stub defined for code contracts.
        /// </remarks>
        IReceivableSourceBlock<IProjectVersionedValue<IProjectTreeSnapshot>> IProjectTreeProvider.Tree 
            => Tree;

        /// <summary>
        /// Gets a value indicating whether a given set of nodes can be copied or moved underneath some given node.
        /// </summary>
        /// <param name="nodes">The set of nodes the user wants to copy or move.</param>
        /// <param name="receiver">
        /// The target node where <paramref name="nodes"/> should be copied or moved to.
        /// May be <c>null</c> to determine whether a given set of nodes could allowably be copied anywhere (not 
        /// necessarily everywhere).
        /// </param>
        /// <param name="deleteOriginal"><c>true</c> for a move operation; <c>false</c> for a copy operation.</param>
        /// <returns><c>true</c> if such a move/copy operation would be allowable; <c>false</c> otherwise.</returns>
        public override bool CanCopy(IImmutableSet<IProjectTree> nodes,
                                     IProjectTree receiver,
                                     bool deleteOriginal = false)
        {
            return false;
        }

        /// <summary>
        /// Gets a value indicating whether deleting a given set of items from the project, and optionally from disk,
        /// would be allowed. 
        /// Note: CanRemove can be called several times since there two types of remove operations:
        ///   - Remove is a command that can remove project tree items form the tree/project but not from disk. 
        ///     For that command requests deleteOptions has DeleteOptions.None flag.
        ///   - Delete is a command that can remove project tree items and form project and from disk. 
        ///     For this command requests deleteOptions has DeleteOptions.DeleteFromStorage flag.
        /// We can potentially support only Remove command here, since we don't remove Dependencies form disk, 
        /// thus we return false when DeleteOptions.DeleteFromStorage is provided.
        /// </summary>
        /// <param name="nodes">The nodes that should be deleted.</param>
        /// <param name="deleteOptions">
        /// A value indicating whether the items should be deleted from disk as well as from the project file.
        /// </param>
        public override bool CanRemove(IImmutableSet<IProjectTree> nodes,
                                       DeleteOptions deleteOptions = DeleteOptions.None)
        {
            if (deleteOptions.HasFlag(DeleteOptions.DeleteFromStorage))
            {
                return false;
            }

            IDependenciesSnapshot snapshot = DependenciesSnapshotProvider.CurrentSnapshot;
            if (snapshot == null)
            {
                return false;
            }

            foreach (IProjectTree node in nodes)
            {
                if (!node.Flags.Contains(DependencyTreeFlags.SupportsRemove))
                {
                    return false;
                }

                string filePath = UnconfiguredProject.GetRelativePath(node.FilePath);
                if (string.IsNullOrEmpty(filePath))
                {
                    continue;
                }

                IDependency dependency = snapshot.FindDependency(filePath, topLevel: true);
                if (dependency == null || dependency.Implicit)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Deletes items from the project, and optionally from disk.
        /// Note: Delete and Remove commands are handled via IVsHierarchyDeleteHandler3, not by
        /// IAsyncCommandGroupHandler and first asks us we CanRemove nodes. If yes then RemoveAsync is called.
        /// We can remove only nodes that are standard and based on project items, i.e. nodes that 
        /// are created by default IProjectDependenciesSubTreeProvider implementations and have 
        /// DependencyNode.GenericDependencyFlags flags and IRule with Context != null, in order to obtain 
        /// node's itemSpec. ItemSpec then used to remove a project item having same Include.
        /// </summary>
        /// <param name="nodes">The nodes that should be deleted.</param>
        /// <param name="deleteOptions">A value indicating whether the items should be deleted from disk as well as 
        /// from the project file.
        /// </param>
        /// <exception cref="InvalidOperationException">Thrown when <see cref="IProjectTreeProvider.CanRemove"/> 
        /// would return <c>false</c> for this operation.</exception>
        public override async Task RemoveAsync(IImmutableSet<IProjectTree> nodes,
                                               DeleteOptions deleteOptions = DeleteOptions.None)
        {
            if (deleteOptions.HasFlag(DeleteOptions.DeleteFromStorage))
            {
                throw new NotSupportedException();
            }

            // Get the list of shared import nodes.
            IEnumerable<IProjectTree> sharedImportNodes = nodes.Where(node =>
                    node.Flags.Contains(DependencyTreeFlags.SharedProjectFlags));

            // Get the list of normal reference Item Nodes (this excludes any shared import nodes).
            IEnumerable<IProjectTree> referenceItemNodes = nodes.Except(sharedImportNodes);

            using (ProjectWriteLockReleaser access = await ProjectLockService.WriteLockAsync())
            {
                Project project = await access.GetProjectAsync(ActiveConfiguredProject);

                // Handle the removal of normal reference Item Nodes (this excludes any shared import nodes).
                foreach (IProjectTree node in referenceItemNodes)
                {
                    if (node.BrowseObjectProperties?.Context == null)
                    {
                        // if node does not have an IRule with valid ProjectPropertiesContext we can not 
                        // get its itemsSpec. If nodes provided by custom IProjectDependenciesSubTreeProvider
                        // implementation, and have some custom IRule without context, it is not a problem,
                        // since they would not have DependencyNode.GenericDependencyFlags and we would not 
                        // end up here, since CanRemove would return false and Remove command would not show 
                        // up for those nodes. 
                        continue;
                    }

                    IProjectPropertiesContext nodeItemContext = node.BrowseObjectProperties.Context;
                    ProjectItem unresolvedReferenceItem = project.GetItemsByEvaluatedInclude(nodeItemContext.ItemName)
                        .FirstOrDefault(item => string.Equals(item.ItemType,
                                                              nodeItemContext.ItemType,
                                                              StringComparison.OrdinalIgnoreCase));

                    Report.IfNot(unresolvedReferenceItem != null, "Cannot find reference to remove.");
                    if (unresolvedReferenceItem != null)
                    {
                        await access.CheckoutAsync(unresolvedReferenceItem.Xml.ContainingProject.FullPath);
                        project.RemoveItem(unresolvedReferenceItem);
                    }
                }

                IDependenciesSnapshot snapshot = DependenciesSnapshotProvider.CurrentSnapshot;
                Requires.NotNull(snapshot, nameof(snapshot));
                if (snapshot == null)
                {
                    return;
                }

                // Handle the removal of shared import nodes.
                ProjectRootElement projectXml = await access.GetProjectXmlAsync(UnconfiguredProject.FullPath);
                foreach (IProjectTree sharedImportNode in sharedImportNodes)
                {
                    string sharedFilePath = UnconfiguredProject.GetRelativePath(sharedImportNode.FilePath);
                    if (string.IsNullOrEmpty(sharedFilePath))
                    {
                        continue;
                    }

                    IDependency sharedProjectDependency = snapshot.FindDependency(sharedFilePath, topLevel: true);
                    if (sharedProjectDependency != null)
                    {
                        sharedFilePath = sharedProjectDependency.Path;
                    }

                    // Find the import that is included in the evaluation of the specified ConfiguredProject that
                    // imports the project file whose full path matches the specified one.
                    IEnumerable<ResolvedImport> matchingImports = from import in project.Imports
                                                                  where import.ImportingElement.ContainingProject == projectXml
                                                                     && PathHelper.IsSamePath(import.ImportedProject.FullPath, sharedFilePath)
                                                                  select import;
                    foreach (ResolvedImport importToRemove in matchingImports)
                    {
                        ProjectImportElement importingElementToRemove = importToRemove.ImportingElement;
                        Report.IfNot(importingElementToRemove != null,
                                     "Cannot find shared project reference to remove.");
                        if (importingElementToRemove != null)
                        {
                            await access.CheckoutAsync(importingElementToRemove.ContainingProject.FullPath);
                            importingElementToRemove.Parent.RemoveChild(importingElementToRemove);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finds dependencies child nodes by their path. We need to override it since
        /// we need to find children under either:
        ///     - our dependencies root node.
        ///     - dependency sub tree nodes
        ///     - dependency sub tree top level nodes
        /// (deeper levels will be graph nodes with additional info, not direct dependencies
        /// specified in the project file)
        /// </summary>
        public override IProjectTree FindByPath(IProjectTree root, string path)
        {
            return ViewProviders.FirstOrDefault()?.Value.FindByPath(root, path);
        }

        /// <summary>
        /// This is still needed for graph nodes search
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public override string GetPath(IProjectTree node)
        {
            return node.FilePath;
        }

        /// <summary>
        /// Generates the original references directory tree.
        /// </summary>
        protected override void Initialize()
        {
            using (UnconfiguredProjectAsynchronousTasksService.LoadedProject())
            {
                base.Initialize();

                // this.IsApplicable may take a project lock, so we can't do it inline with this method
                // which is holding a private lock.  It turns out that doing it asynchronously isn't a problem anyway,
                // so long as we guard against races with the Dispose method.
                UnconfiguredProjectAsynchronousTasksService.LoadedProjectAsync(
                    async delegate
                    {
                        await TaskScheduler.Default.SwitchTo(alwaysYield: true);
                        UnconfiguredProjectAsynchronousTasksService
                            .UnloadCancellationToken.ThrowIfCancellationRequested();

                        lock (SyncObject)
                        {
                            DependenciesSnapshotProvider.SnapshotChanged += OnDependenciesSnapshotChanged;

                            Verify.NotDisposed(this);
                            Task<IProjectVersionedValue<IProjectTreeSnapshot>> nowait = SubmitTreeUpdateAsync(
                                (treeSnapshot, configuredProjectExports, cancellationToken) =>
                                {
                                    IProjectTree dependenciesNode = CreateDependenciesFolder();

                                    // TODO create providers nodes that can be visible when empty
                                    //dependenciesNode = CreateOrUpdateSubTreeProviderNodes(dependenciesNode, 
                                    //                                                      cancellationToken);

                                    return Task.FromResult(new TreeUpdateResult(dependenciesNode, true));
                                });
                        }

                    },
                    registerFaultHandler: true);
            }

            IProjectTree CreateDependenciesFolder()
            {
                var values = new ReferencesProjectTreeCustomizablePropertyValues
                {
                    Caption = VSResources.DependenciesNodeName,
                    Icon = ManagedImageMonikers.ReferenceGroup.ToProjectSystemType(),
                    ExpandedIcon = ManagedImageMonikers.ReferenceGroup.ToProjectSystemType(),
                    Flags = DependencyTreeFlags.DependenciesRootNodeFlags
                };

                // Allow property providers to perform customization
                foreach (IProjectTreePropertiesProvider provider in ProjectTreePropertiesProviders.ExtensionValues())
                {
                    provider.CalculatePropertyValues(null, values);
                }

                // Note that all the parameters are specified so we can force this call to an
                // overload of NewTree available prior to 15.5 versions of CPS. Once a 15.5 build
                // is publicly available we can move this to an overload with default values for
                // most of the parameters, and we'll only need to pass the interesting ones.
                return NewTree(
                    caption: values.Caption,
                    filePath: null,
                    browseObjectProperties: null,
                    icon: values.Icon,
                    expandedIcon: values.ExpandedIcon,
                    visible: true,
                    flags: values.Flags);
            }
        }

        private void OnDependenciesSnapshotChanged(object sender, SnapshotChangedEventArgs e)
        {
            IDependenciesSnapshot snapshot = e.Snapshot;
            if (snapshot == null)
            {
                return;
            }

            lock (_treeUpdateLock)
            {
                if (_treeUpdateQueueTask == null || _treeUpdateQueueTask.IsCompleted)
                {
                    _treeUpdateQueueTask = ThreadingService.JoinableTaskFactory.RunAsync(async () =>
                    {
                        if (TasksService.UnloadCancellationToken.IsCancellationRequested)
                        {
                            return Task.CompletedTask;
                        }

                        BuildTreeForSnapshot();

                        return Task.CompletedTask;
                    }).Task;
                }
                else
                {
                    _treeUpdateQueueTask = _treeUpdateQueueTask.ContinueWith(
                        t => BuildTreeForSnapshot(), TaskScheduler.Default);
                }
            }

            return;

            void BuildTreeForSnapshot()
            {
                Lazy<IDependenciesTreeViewProvider, IOrderPrecedenceMetadataView> viewProvider = ViewProviders.FirstOrDefault();

                if (viewProvider == null)
                {
                    return;
                }

                Task<IProjectVersionedValue<IProjectTreeSnapshot>> nowait = SubmitTreeUpdateAsync(
                    async (treeSnapshot, configuredProjectExports, cancellationToken) =>
                    {
                        IProjectTree dependenciesNode = treeSnapshot.Value.Tree;
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            dependenciesNode = await viewProvider.Value.BuildTreeAsync(dependenciesNode, snapshot, cancellationToken);

                            _treeTelemetryService.ObserveTreeUpdateCompleted(snapshot.HasUnresolvedDependency);
                        }

                        // TODO We still are getting mismatched data sources and need to figure out better 
                        // way of merging, mute them for now and get to it in U1
                        return new TreeUpdateResult(dependenciesNode, false, null);
                    });
            }
        }

        /// <summary>
        /// Creates a new instance of the configured project exports class.
        /// </summary>
        protected override ConfiguredProjectExports GetActiveConfiguredProjectExports(
                                ConfiguredProject newActiveConfiguredProject)
        {
            Requires.NotNull(newActiveConfiguredProject, nameof(newActiveConfiguredProject));

            return GetActiveConfiguredProjectExports<MyConfiguredProjectExports>(newActiveConfiguredProject);
        }

        #region IDependencyTreeServices

        public IProjectTree CreateTree(
            string caption,
            IProjectPropertiesContext itemContext,
            IPropertySheet propertySheet = null,
            IRule browseObjectProperties = null,
            ProjectImageMoniker icon = null,
            ProjectImageMoniker expandedIcon = null,
            bool visible = true,
            ProjectTreeFlags? flags = default)
        {
            // Note that all the parameters are specified so we can force this call to an
            // overload of NewTree available prior to 15.5 versions of CPS. Once a 15.5 build
            // is publicly available we can move this to an overload with default values for
            // most of the parameters, and we'll only need to pass the interesting ones.
            return NewTree(
                caption: caption,
                item: itemContext,
                propertySheet: propertySheet,
                browseObjectProperties: browseObjectProperties,
                icon: icon,
                expandedIcon: expandedIcon,
                visible: visible,
                flags: flags,
                isLinked: false);
        }

        public IProjectTree CreateTree(
            string caption,
            string filePath,
            IRule browseObjectProperties = null,
            ProjectImageMoniker icon = null,
            ProjectImageMoniker expandedIcon = null,
            bool visible = true,
            ProjectTreeFlags? flags = default)
        {
            return NewTree(
                caption: caption,
                filePath: filePath,
                browseObjectProperties: browseObjectProperties,
                icon: icon,
                expandedIcon: expandedIcon,
                visible: visible,
                flags: flags);
        }

        public async Task<IRule> GetRuleAsync(IDependency dependency, IProjectCatalogSnapshot catalogs)
        {
            Requires.NotNull(dependency, nameof(dependency));

            ConfiguredProject project = dependency.TargetFramework.Equals(TargetFramework.Any)
                ? ActiveConfiguredProject
                : await DependenciesHost.GetConfiguredProject(dependency.TargetFramework) ?? ActiveConfiguredProject;

            ConfiguredProjectExports configuredProjectExports = GetActiveConfiguredProjectExports(project);
            IImmutableDictionary<string, IPropertyPagesCatalog> namedCatalogs = await GetNamedCatalogsAsync();
            Requires.NotNull(namedCatalogs, nameof(namedCatalogs));

            IPropertyPagesCatalog browseObjectsCatalog = namedCatalogs[PropertyPageContexts.BrowseObject];
            Rule schema = browseObjectsCatalog.GetSchema(dependency.SchemaName);
            string itemSpec = string.IsNullOrEmpty(dependency.OriginalItemSpec) ? dependency.Path : dependency.OriginalItemSpec;
            var context = ProjectPropertiesContext.GetContext(UnconfiguredProject,
                itemType: dependency.SchemaItemType,
                itemName: itemSpec);

            if (schema == null)
            {
                // Since we have no browse object, we still need to create *something* so
                // that standard property pages can pop up.
                Rule emptyRule = RuleExtensions.SynthesizeEmptyRule(context.ItemType);
                return configuredProjectExports.PropertyPagesDataModelProvider.GetRule(
                    emptyRule,
                    context.File,
                    context.ItemType,
                    context.ItemName);
            }

            if (dependency.Resolved)
            {
                return configuredProjectExports.RuleFactory.CreateResolvedReferencePageRule(
                    schema,
                    context,
                    dependency.Name,
                    dependency.Properties);
            }

            return browseObjectsCatalog.BindToContext(schema.Name, context);

            async Task<IImmutableDictionary<string, IPropertyPagesCatalog>> GetNamedCatalogsAsync()
            {
                if (catalogs != null)
                {
                    return catalogs.NamedCatalogs;
                }

                if (NamedCatalogs == null)
                {
                    // Note: it is unlikely that we end up here, however for cases when node providers
                    // getting their node data not from Design time build events, we might have OnDependenciesChanged
                    // event coming before initial design time build event updates NamedCatalogs in this class.
                    // Thus, just in case, explicitly request it here (GetCatalogsAsync will acquire a project read lock)
                    NamedCatalogs = await ActiveConfiguredProject.Services
                        .PropertyPagesCatalog
                        .GetCatalogsAsync(CancellationToken.None);
                }

                return NamedCatalogs;
            }
        }

        #endregion

        /// <summary>
        /// Describes services collected from the active configured project.
        /// </summary>
        [Export]
        protected class MyConfiguredProjectExports : ConfiguredProjectExports
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="MyConfiguredProjectExports"/> class.
            /// </summary>
            [ImportingConstructor]
            protected MyConfiguredProjectExports(ConfiguredProject configuredProject)
                : base(configuredProject)
            {
            }
        }
    }
}
