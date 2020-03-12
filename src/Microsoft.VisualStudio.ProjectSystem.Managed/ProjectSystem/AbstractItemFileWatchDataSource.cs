﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Base class for producing items so that they can be watched, and when changed, trigger re-evaluation.
    /// </summary>
    internal abstract class AbstractItemFileWatchDataSource : ChainedProjectValueDataSourceBase<FileWatchData>, IFileWatchDataSource
    {
        private readonly ConfiguredProject _project;
        private readonly IProjectSubscriptionService _projectSubscriptionService;

        protected AbstractItemFileWatchDataSource(ConfiguredProject project, IProjectSubscriptionService projectSubscriptionService)
            : base(project.Services, synchronousDisposal: true, registerDataSource: false)
        {
            _project = project;
            _projectSubscriptionService = projectSubscriptionService;
        }

        public abstract string ItemSchemaName { get; }

        public abstract string FullPathProperty { get; }

        public abstract FileWatchChangeKinds FileWatchChangeKinds { get; }

        protected override UnconfiguredProject ContainingProject
        {
            get { return _project.UnconfiguredProject; }
        }

        protected override IDisposable LinkExternalInput(ITargetBlock<IProjectVersionedValue<FileWatchData>> targetBlock)
        {
            IProjectValueDataSource<IProjectSubscriptionUpdate> source = _projectSubscriptionService.ProjectRuleSource;

            // Transform the changes from evaluation -> FileWatchData
            DisposableValue<ISourceBlock<IProjectVersionedValue<FileWatchData>>> transformBlock = source.SourceBlock
                                                                                                        .TransformWithNoDelta(update => update.Derive(u => CreateFileWatch(u.CurrentState)),
                                                                                                            suppressVersionOnlyUpdates: true,
                                                                                                            ruleNames: new[] { ItemSchemaName });

            // Set the link up so that we publish changes to target block
            transformBlock.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

            // Join the source blocks, so if they need to switch to UI thread to complete 
            // and someone is blocked on us on the same thread, the call proceeds
            JoinUpstreamDataSources(source);

            return transformBlock;
        }

        private FileWatchData CreateFileWatch(IImmutableDictionary<string, IProjectRuleSnapshot> update)
        {
            IProjectRuleSnapshot snapshot = update.GetSnapshotOrEmpty(ItemSchemaName);

            var fullPaths = snapshot.Items.Select(item => item.Value.GetValueOrDefault(Compile.FullPathProperty))
                                          .WhereNotNull()
                                          .ToImmutableList();

            return new FileWatchData(
                this,
                fullPaths,
                FileWatchChangeKinds);
        }
    }
}
