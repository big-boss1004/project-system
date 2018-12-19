﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.IO;

using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.ViewProviders;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.Actions
{
    internal abstract class GraphActionHandlerBase : IDependenciesGraphActionHandler
    {
        protected GraphActionHandlerBase(IDependenciesGraphBuilder builder,
                                      IAggregateDependenciesSnapshotProvider aggregateSnapshotProvider)
        {
            Builder = builder;
            AggregateSnapshotProvider = aggregateSnapshotProvider;
            ViewProviders = new OrderPrecedenceImportCollection<IDependenciesGraphViewProvider>(
                                    ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst);
        }

        protected IDependenciesGraphBuilder Builder { get; }
        protected IAggregateDependenciesSnapshotProvider AggregateSnapshotProvider { get; }

        [ImportMany]
        protected OrderPrecedenceImportCollection<IDependenciesGraphViewProvider> ViewProviders { get; }

        public abstract bool CanHandleRequest(IGraphContext graphContext);

        public abstract bool HandleRequest(IGraphContext graphContext);

        protected IDependency GetDependency(GraphNode inputGraphNode, out IDependenciesSnapshot snapshot)
        {
            string projectPath = inputGraphNode.Id.GetValue(CodeGraphNodeIdName.Assembly);
            if (string.IsNullOrWhiteSpace(projectPath))
            {
                snapshot = null;
                return null;
            }

            string projectFolder = Path.GetDirectoryName(projectPath);
            if (projectFolder == null)
            {
                snapshot = null;
                return null;
            }

            string id = inputGraphNode.GetValue<string>(DependenciesGraphSchema.DependencyIdProperty);

            bool topLevel;

            if (id == null)
            {
                // this is top level node and it contains full path 
                id = inputGraphNode.Id.GetValue(CodeGraphNodeIdName.File);

                if (id == null)
                {
                    // No full path, so this must be a node generated by a different provider.
                    snapshot = null;
                    return null;
                }

                if (id.StartsWith(projectFolder, StringComparison.OrdinalIgnoreCase))
                {
                    int startIndex = projectFolder.Length;
                    
                    // Trim backslashes (without allocating)
                    while (startIndex < id.Length && id[startIndex] == '\\')
                    {
                        startIndex++;
                    }

                    id = id.Substring(startIndex);
                }

                topLevel = true;
            }
            else
            {
                topLevel = false;
            }

            snapshot = AggregateSnapshotProvider.GetSnapshotProvider(projectPath)?.CurrentSnapshot;

            return snapshot?.FindDependency(id, topLevel);
        }
    }
}
