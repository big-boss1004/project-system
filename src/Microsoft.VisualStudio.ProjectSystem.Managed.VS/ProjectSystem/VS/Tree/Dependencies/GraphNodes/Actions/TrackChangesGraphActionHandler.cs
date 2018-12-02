﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Linq;

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.ViewProviders;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.Actions
{
    [Export(typeof(IDependenciesGraphActionHandler))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal class TrackChangesGraphActionHandler : GraphActionHandlerBase
    {
        public const int Order = 130;

        [ImportingConstructor]
        public TrackChangesGraphActionHandler(IDependenciesGraphBuilder builder,
                                              IAggregateDependenciesSnapshotProvider aggregateSnapshotProvider)
            : base(builder, aggregateSnapshotProvider)
        {
        }

        public override bool CanHandleChanges()
        {
            return true;
        }

        public override bool HandleChanges(IGraphContext graphContext, SnapshotChangedEventArgs e)
        {
            IDependenciesSnapshot snapshot = e.Snapshot;

            if (snapshot == null || e.Token.IsCancellationRequested)
            {
                return false;
            }

            foreach (GraphNode inputGraphNode in graphContext.InputNodes.ToList())
            {
                string existingDependencyId = inputGraphNode.GetValue<string>(DependenciesGraphSchema.DependencyIdProperty);
                if (string.IsNullOrEmpty(existingDependencyId))
                {
                    continue;
                }

                string projectPath = inputGraphNode.Id.GetValue(CodeGraphNodeIdName.Assembly);
                if (string.IsNullOrEmpty(projectPath))
                {
                    continue;
                }

                IDependency updatedDependency = GetDependency(projectPath, existingDependencyId, out IDependenciesSnapshot updatedSnapshot);
                if (updatedDependency == null)
                {
                    continue;
                }

                IDependenciesGraphViewProvider viewProvider = ViewProviders
                    .FirstOrDefaultValue((x, d) => x.SupportsDependency(d), updatedDependency);
                if (viewProvider == null)
                {
                    continue;
                }

                if (!viewProvider.ShouldTrackChanges(projectPath, snapshot.ProjectPath, updatedDependency))
                {
                    continue;
                }

                using (var scope = new GraphTransactionScope())
                {
                    viewProvider.TrackChanges(
                        graphContext,
                        projectPath,
                        updatedDependency,
                        inputGraphNode,
                        updatedSnapshot.Targets[updatedDependency.TargetFramework]);

                    scope.Complete();
                }
            }

            return false;
        }
    }
}
