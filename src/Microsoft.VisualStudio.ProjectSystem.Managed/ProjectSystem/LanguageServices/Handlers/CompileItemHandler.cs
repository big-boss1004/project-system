﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.Logging;
using Microsoft.VisualStudio.ProjectSystem.Rename;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to Compile items during project evaluations and items that are passed
    ///     to the compiler during design-time builds.
    /// </summary>
    [Export(typeof(IWorkspaceContextHandler))]
    internal class CompileItemHandler : AbstractEvaluationCommandLineHandler, IProjectEvaluationHandler, ICommandLineHandler, IProjectUpdatedHandler
    {
        private readonly UnconfiguredProject _project;

        [ImportingConstructor]
        public CompileItemHandler(UnconfiguredProject project)
            : base(project)
        {
            _project = project;
        }

        [ImportMany]
        private readonly IEnumerable<IFileRenameHandler> _fileRenameHandlers = null!;

        public string ProjectEvaluationRule
        {
            get { return Compile.SchemaName; }
        }

        public void Handle(IComparable version, IProjectChangeDescription projectChange, bool isActiveContext, IProjectLogger logger)
        {
            Requires.NotNull(version, nameof(version));
            Requires.NotNull(projectChange, nameof(projectChange));
            Requires.NotNull(logger, nameof(logger));

            VerifyInitialized();

            ApplyProjectEvaluation(version, projectChange.Difference, projectChange.Before.Items, projectChange.After.Items, isActiveContext, logger);
        }

        public void Handle(IComparable version, BuildOptions added, BuildOptions removed, bool isActiveContext, IProjectLogger logger)
        {
            Requires.NotNull(version, nameof(version));
            Requires.NotNull(added, nameof(added));
            Requires.NotNull(removed, nameof(removed));
            Requires.NotNull(logger, nameof(logger));

            VerifyInitialized();

            IProjectChangeDiff difference = ConvertToProjectDiff(added, removed);

            ApplyProjectBuild(version, difference, isActiveContext, logger);
        }

        public void HandleProjectUpdate(IComparable version, IProjectChangeDescription projectChange, IProjectLogger logger)
        {
            Requires.NotNull(version, nameof(version));
            Requires.NotNull(projectChange, nameof(projectChange));
            Requires.NotNull(logger, nameof(logger));

            VerifyInitialized();

            foreach ((string original, string renamed) in projectChange.Difference.RenamedItems)
            {
                HandleItemRename(original, renamed, logger);
            }
        }

        protected override void AddToContext(string fullPath, IImmutableDictionary<string, string> metadata, bool isActiveContext, IProjectLogger logger)
        {
            string[]? folderNames = FileItemServices.GetLogicalFolderNames(Path.GetDirectoryName(_project.FullPath), fullPath, metadata);

            logger.WriteLine("Adding source file '{0}'", fullPath);
            Context.AddSourceFile(fullPath, isInCurrentContext: isActiveContext, folderNames: folderNames);
        }

        protected override void RemoveFromContext(string fullPath, IProjectLogger logger)
        {
            logger.WriteLine("Removing source file '{0}'", fullPath);
            Context.RemoveSourceFile(fullPath);
        }

        protected override void UpdateInContext(string fullPath, IImmutableDictionary<string, string> previousMetadata, IImmutableDictionary<string, string> currentMetadata, bool isActiveContext, IProjectLogger logger)
        {
            if (LinkMetadataChanged(previousMetadata, currentMetadata))
            {
                logger.WriteLine("Removing and then re-adding source file '{0}' to <Link> metadata changes", fullPath);
                RemoveFromContext(fullPath, logger);
                AddToContext(fullPath, currentMetadata, isActiveContext, logger);
            }
        }

        private static bool LinkMetadataChanged(IImmutableDictionary<string, string> previousMetadata, IImmutableDictionary<string, string> currentMetadata)
        {
            string previousLink = previousMetadata.GetValueOrDefault(Compile.LinkProperty, string.Empty);
            string currentLink = currentMetadata.GetValueOrDefault(Compile.LinkProperty, string.Empty);

            return previousLink != currentLink;
        }

        protected override void HandleItemRename(string fullPathBefore, string fullPathAfter, IProjectLogger logger)
        {
            logger.WriteLine("Handling rename of source file '{0}' to {1}", fullPathBefore, fullPathAfter);
            foreach (IFileRenameHandler fileRenameHandler in _fileRenameHandlers)
            {
                fileRenameHandler.HandleRename(fullPathBefore, fullPathAfter);
            }
        }

        private IProjectChangeDiff ConvertToProjectDiff(BuildOptions added, BuildOptions removed)
        {
            var addedSet = GetFilePaths(added).ToImmutableHashSet(StringComparers.Paths);
            var removedSet = GetFilePaths(removed).ToImmutableHashSet(StringComparers.Paths);

            return new ProjectChangeDiff(addedSet, removedSet);
        }

        private IEnumerable<string> GetFilePaths(BuildOptions options)
        {
            return options.SourceFiles.Select(f => _project.MakeRelative(f.Path));
        }
    }
}
