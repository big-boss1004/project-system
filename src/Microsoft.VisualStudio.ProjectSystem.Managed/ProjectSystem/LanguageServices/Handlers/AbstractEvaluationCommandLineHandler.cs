﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.VisualStudio.ProjectSystem.Logging;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Responsible for coordinating changes and conflicts between evaluation and design-time builds, and pushing those changes
    ///     onto Roslyn via a <see cref="IWorkspaceProjectContext"/>.
    /// </summary>
    internal abstract partial class AbstractEvaluationCommandLineHandler
    {
        // This class is not thread-safe, and the assumption is that the caller will mkaes sure that evaluations and design-time builds do 
        // overlap inside the class at the same time.
        //
        // In the ideal world, we would simply wait for a design-time build to get the command-line arguments that would have been passed
        // to Csc/Vbc and push these onto Roslyn. This is exactly what the legacy project system did; when a user added or removed a file
        // or changed the project, it performed a blocking wait on the design-time build before returning control to the user. In CPS,
        // however, design-time builds are not UI blocking, so control can be returned to the user before Roslyn has been told about the 
        // file. This leads to the user observable behavior where the source file for a period of time lives in the "Misc" project and is 
        // without "project" IntelliSense. To counteract that, we push changes both in design-time builds *and* during evaluations, which 
        // gives the user results a lot faster than if we just pushed during design-time builds only.
        //
        // Typically, adds and removes of files found at evaluation time are also found during a design-time build, with the later also 
        // including generated files. This forces us to remember what files we've already sent to Roslyn to avoid sending duplicate adds
        // or removes of the same file. Due to design-time builds being significantly slower than evaluations, there are also times where 
        // many evaluations have occured by the time a design-time build based on a past version of the ConfiguredProject has completed.
        // This can lead to conflicts.
        //
        // A conflict occurs when evaluation or design-time build adds a item that the other removed, or vice versa. 
        // 
        //  Examples of conflicts include:
        //
        //   - A user removes a item before a design-time build that contains the addition of that item has finished
        //   - A user adds a item before a design-time build that contains the removoal of that item has finished
        //   - A user adds a item that was previously generated by a target (but stopped generating it)
        //   - A user removes a item and in the same version it starts getting generated via a target during design-time build
        //
        //  Examples of changes that are not conflicts include:
        // 
        //   - A user adds a item and it appears as an addition in both evaluation and design-time build (the item is always added)
        //   - A user removes a item and it appears as removal in both evaluation and design-time build  (the item is always removed)
        //   - A target during design-time build generates an item that did not appear during evaluation (the item is always added)
        //   - A target, new since the last design-time build, removes a item that appeared during evaluation (the item is always removed)
        //
        // TODO: These are also not conflicts, but we're currently handling differently to a normal build, which we should fix:
        //
        //    - A target, since the very first design-time build, removed a item that appeared during evaluation (currently the item gets added).
        //          * This is because a design-time build IProjectChangeDescription is only a diff between itself and the previous build, 
        //            not between itself and evaluation, which means that design-time build diff never knows that the item was removed.
        //
        // Algorithm for resolving conflicts is as follows:
        //
        // 1. Walk every evaluation since the last design-time build, discarding those from conflict resolution that have a version less 
        //    than or equal to the current design-time build. 
        // 2. Walk every design-time build addition, if there's an associated removal in a later evaluation - we throw away the addition
        // 3. Walk every design-time build removal, if there's an associated addition in a later evaluation - we throw away the removal
        //
        // We don't resolve conflicts between changes items, because the design-time build doesn't produce them due to the way we represent
        // command-line arguments as individual item includes, such as <CscCommandLineArguments Include="/reference:Foo.dll"/>, without any 
        // metadata.
        //
        private readonly HashSet<string> _paths = new HashSet<string>(StringComparers.Paths);
        private readonly Queue<VersionedProjectChangeDiff> _evaluations = new Queue<VersionedProjectChangeDiff>();
        private readonly UnconfiguredProject _project;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AbstractEvaluationCommandLineHandler"/> class with the specified project.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="project"/> is <see langword="null"/>.
        /// </exception>
        protected AbstractEvaluationCommandLineHandler(UnconfiguredProject project)
        {
            Requires.NotNull(project, nameof(project));

            _project = project;
        }

        /// <summary>
        ///     Applys the specified version of evaluation <see cref="IProjectChangeDiff"/> and metadata to the underlying 
        ///     <see cref="IWorkspaceProjectContext"/>, indicating if the context is the currently active one.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="version"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="difference" /> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="metadata" /> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="logger" /> is <see langword="null"/>.
        /// </exception>
        public void ApplyEvaluationChanges(IComparable version, IProjectChangeDiff difference, IImmutableDictionary<string, IImmutableDictionary<string, string>> metadata, bool isActiveContext, IProjectLogger logger)
        {
            Requires.NotNull(version, nameof(version));
            Requires.NotNull(version, nameof(version));
            Requires.NotNull(metadata, nameof(metadata));
            Requires.NotNull(logger, nameof(logger));

            if (!difference.AnyChanges)
                return;

            difference = NormalizeDifferences(difference);
            EnqueueEvaluation(version, difference);

            ApplyChangesToContext(version, difference, metadata, isActiveContext, logger);
        }

        /// <summary>
        ///     Applys the specified version of design-time build <see cref="IProjectChangeDiff"/> to the underlying
        ///     <see cref="IWorkspaceProjectContext"/>, indicating if the context is the currently active one.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="version"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="difference" /> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="logger" /> is <see langword="null"/>.
        /// </exception>
        public void ApplyDesignTimeChanges(IComparable version, IProjectChangeDiff difference, bool isActiveContext, IProjectLogger logger)
        {
            Requires.NotNull(version, nameof(version));
            Requires.NotNull(difference, nameof(difference));
            Requires.NotNull(logger, nameof(logger));

            if (!difference.AnyChanges)
                return;

            difference = NormalizeDifferences(difference);
            difference = ResolveDesignTimeConflicts(version, difference);

            ApplyChangesToContext(version, difference, ImmutableDictionary<string, IImmutableDictionary<string, string>>.Empty, isActiveContext, logger);
        }

        protected abstract void AddToContext(string fullPath, IImmutableDictionary<string, string> metadata, bool isActiveContext, IProjectLogger logger);

        protected abstract void RemoveFromContext(string fullPath, IProjectLogger logger);

        private void ApplyChangesToContext(IComparable version, IProjectChangeDiff difference, IImmutableDictionary<string, IImmutableDictionary<string, string>> metadata, bool isActiveContext, IProjectLogger logger)
        {
            foreach (string includePath in difference.RemovedItems)
            {
                RemoveFromContextIfPresent(includePath, logger);
            }

            foreach (string includePath in difference.AddedItems)
            {
                AddToContextIfNotPresent(includePath, metadata, isActiveContext, logger);
            }

            // We Remove then Add changed items to pick up the Linked metadata
            foreach (string includePath in difference.ChangedItems)
            {
                RemoveFromContextIfPresent(includePath, logger);
                AddToContextIfNotPresent(includePath, metadata, isActiveContext, logger);
            }

            Assumes.True(difference.RenamedItems.Count == 0, "We should have normalized renames.");
        }

        private void RemoveFromContextIfPresent(string includePath, IProjectLogger logger)
        {
            string fullPath = _project.MakeRooted(includePath);

            // Remove from the context first so if Roslyn throws due to a bug 
            // or other reason, that our state of the world remains consistent
            if (_paths.Contains(fullPath))
            {
                RemoveFromContext(fullPath, logger);
                bool removed = _paths.Remove(fullPath);
                Assumes.True(removed);
            }
        }

        private void AddToContextIfNotPresent(string includePath, IImmutableDictionary<string, IImmutableDictionary<string, string>> metadata, bool isActiveContext, IProjectLogger logger)
        {
            string fullPath = _project.MakeRooted(includePath);

            // Add to the context first so if Roslyn throws due to a bug or
            // other reason, that our state of the world remains consistent
            if (!_paths.Contains(fullPath))
            {
                var itemMetadata = metadata.GetValueOrDefault(includePath, ImmutableDictionary<string, string>.Empty);
                AddToContext(fullPath, itemMetadata, isActiveContext, logger);
                bool added = _paths.Add(fullPath);
                Assumes.True(added);
            }
        }

        private IProjectChangeDiff ResolveDesignTimeConflicts(IComparable designTimeVersion, IProjectChangeDiff designTimeDifference)
        {
            DiscardOutOfDateEvaluations(designTimeVersion);

            // Walk all evaluations (if any) that occurred since we launched and resolve the conflicts
            foreach (VersionedProjectChangeDiff evaluation in _evaluations)
            {
                Assumes.True(evaluation.Version.IsLaterThan(designTimeVersion), "Attempted to resolve a conflict between a design-time build and an earlier evaluation.");

                designTimeDifference = ResolveConflicts(evaluation.Difference, designTimeDifference);
            }

            return designTimeDifference;
        }

        private IProjectChangeDiff ResolveConflicts(IProjectChangeDiff evaluationDifferences, IProjectChangeDiff designTimeDifferences)
        {
            // Remove added items that were removed by later evaluations, and vice versa
            IImmutableSet<string> added = designTimeDifferences.AddedItems.Except(evaluationDifferences.RemovedItems);
            IImmutableSet<string> removed = designTimeDifferences.RemovedItems.Except(evaluationDifferences.AddedItems);

            Assumes.True(designTimeDifferences.ChangedItems.Count == 0, "We should never see ChangedItems during design-time builds.");

            return new ProjectChangeDiff(added, removed, designTimeDifferences.ChangedItems);
        }

        private void DiscardOutOfDateEvaluations(IComparable version)
        {
            // Throw away evaluations that are the same version or earlier than the design-time build
            // version as it has more up-to-date information on the the current state of the project

            // Note, evaluations could be empty if previous evaluations resulted in no new changes
            while (_evaluations.Count > 0)
            {
                VersionedProjectChangeDiff evaluation = _evaluations.Peek();
                if (evaluation.Version.IsEarlierThanOrEqualTo(version))
                {
                    _evaluations.Dequeue();
                }
            }
        }

        private void EnqueueEvaluation(IComparable version, IProjectChangeDiff evaluationDifference)
        {
            Assumes.False(_evaluations.Count > 0 && version.IsEarlierThanOrEqualTo(_evaluations.Peek().Version), "Attempted to push an evaluation that regressed in version.");

            _evaluations.Enqueue(new VersionedProjectChangeDiff(version, evaluationDifference));
        }

        private IProjectChangeDiff NormalizeDifferences(IProjectChangeDiff difference)
        {
            // Optimize for common case
            if (difference.RenamedItems.Count == 0)
                return difference;

            // Treat renamed items as just as an Add and Remove, makes finding conflicts easier
            IEnumerable<string> renamedNewNames = difference.RenamedItems.Select(r => r.Value);
            IEnumerable<string> renamedOldNames = difference.RenamedItems.Select(e => e.Key);

            IImmutableSet<string> added = difference.AddedItems.Union(renamedNewNames);
            IImmutableSet<string> removed = difference.RemovedItems.Union(renamedOldNames);

            return new ProjectChangeDiff(added, removed, difference.ChangedItems);
        }
    }
}
