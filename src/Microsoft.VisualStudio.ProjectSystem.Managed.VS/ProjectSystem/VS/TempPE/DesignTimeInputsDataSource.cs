﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    /// <summary>
    /// Processes Compile source items into <see cref="DesignTimeInputs" /> that includes design time and shared design time inputs
    /// only.
    /// </summary>
    [Export(typeof(IDesignTimeInputsDataSource))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicLanguageService)]
    internal class DesignTimeInputsDataSource : ChainedProjectValueDataSourceBase<DesignTimeInputs>, IDesignTimeInputsDataSource
    {
        private static readonly ImmutableHashSet<string> s_ruleNames = Empty.OrdinalIgnoreCaseStringSet.Add(Compile.SchemaName);

        private readonly UnconfiguredProject _project;
        private readonly IProjectSubscriptionService _projectSubscriptionService;

        [ImportingConstructor]
        public DesignTimeInputsDataSource(ConfiguredProject project, IProjectSubscriptionService projectSubscriptionService)
            : base(project.Services, synchronousDisposal: true, registerDataSource: false)
        {
            _project = project.UnconfiguredProject;
            _projectSubscriptionService = projectSubscriptionService;
        }

        protected override UnconfiguredProject ContainingProject
        {
            get { return _project; }
        }

        /// <summary>
        /// Allow unit tests to initialize this class. May be removed in future
        /// </summary>
        internal void Test_Initialize()
        {
            EnsureInitialized();
        }

        protected override IDisposable LinkExternalInput(ITargetBlock<IProjectVersionedValue<DesignTimeInputs>> targetBlock)
        {
            IProjectValueDataSource<IProjectSubscriptionUpdate> source = _projectSubscriptionService.SourceItemsRuleSource;

            // Transform the changes from evaluation/design-time build -> restore data
            DisposableValue<ISourceBlock<IProjectVersionedValue<DesignTimeInputs>>> transformBlock = source.SourceBlock
                                                                                .TransformWithNoDelta(update => update.Derive(u => GetDesignTimeInputs(u.CurrentState)),
                                                                                                      suppressVersionOnlyUpdates: false,
                                                                                                      ruleNames: s_ruleNames);

            // Set the link up so that we publish changes to target block
            transformBlock.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

            // Join the source blocks, so if they need to switch to UI thread to complete 
            // and someone is blocked on us on the same thread, the call proceeds
            JoinUpstreamDataSources(source);

            return transformBlock;
        }

        private DesignTimeInputs GetDesignTimeInputs(IImmutableDictionary<string, IProjectRuleSnapshot> currentState)
        {
            var designTimeInputs = new List<string>();
            var designTimeSharedInputs = new List<string>();

            foreach ((string itemName, IImmutableDictionary<string, string> metadata) in currentState["Compile"].Items)
            {
                (bool designTime, bool designTimeShared) = GetDesignTimePropsForItem(metadata);

                if (designTime)
                {
                    designTimeInputs.Add(itemName);
                }

                // Legacy allows files to be DesignTime and DesignTimeShared
                if (designTimeShared)
                {
                    designTimeSharedInputs.Add(itemName);

                }
            }

            return new DesignTimeInputs
            {
                Inputs = designTimeInputs.ToArray(),
                SharedInputs = designTimeSharedInputs.ToArray()
            };
        }

        private static (bool designTime, bool designTimeShared) GetDesignTimePropsForItem(IImmutableDictionary<string, string> item)
        {
            item.TryGetValue(Compile.LinkProperty, out string linkString);
            item.TryGetValue(Compile.DesignTimeProperty, out string designTimeString);
            item.TryGetValue(Compile.DesignTimeSharedInputProperty, out string designTimeSharedString);

            if (linkString != null && linkString.Length > 0)
            {
                // Linked files are never used as TempPE inputs
                return (false, false);
            }

            return (StringComparers.PropertyLiteralValues.Equals(designTimeString, bool.TrueString), StringComparers.PropertyLiteralValues.Equals(designTimeSharedString, bool.TrueString));
        }
    }
}
