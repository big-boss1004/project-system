﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rename
{
    internal sealed class SimpleRenameStrategy : AbstractRenameStrategy
    {
        public SimpleRenameStrategy(
            IProjectThreadingService threadingService,
            IUserNotificationServices userNotificationService,
            IEnvironmentOptions environmentOptions,
            IRoslynServices roslynServices)
            : base(threadingService, userNotificationService, environmentOptions, roslynServices)
        {
        }

        // For the SimpleRename, it can attempt to handle any situation
        public override bool CanHandleRename(string oldFileName, string newFileName, bool isCaseSensitive)
        {
            string oldNameBase = Path.GetFileNameWithoutExtension(oldFileName);
            string newNameBase = Path.GetFileNameWithoutExtension(newFileName);
            return _roslynServices.IsValidIdentifier(oldNameBase) && _roslynServices.IsValidIdentifier(newNameBase) && (!string.Equals(Path.GetFileName(oldNameBase), Path.GetFileName(newNameBase), isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));
        }

        public override async Task RenameAsync(Project myNewProject, string oldFileName, string newFileName, bool isCaseSensitive)
        {
            string oldNameBase = Path.GetFileNameWithoutExtension(oldFileName);
            Solution renamedSolution = await GetRenamedSolutionAsync(myNewProject, oldNameBase, newFileName, isCaseSensitive);
            if (renamedSolution == null)
                return;

            await _threadingService.SwitchToUIThread();
            bool renamedSolutionApplied = _roslynServices.ApplyChangesToSolution(myNewProject.Solution.Workspace, renamedSolution);

            if (!renamedSolutionApplied)
            {
                string failureMessage = string.Format(CultureInfo.CurrentCulture, VSResources.RenameSymbolFailed, oldNameBase);
                await _threadingService.SwitchToUIThread();
                _userNotificationServices.ShowWarning(failureMessage);
            }
        }

        private async Task<Solution> GetRenamedSolutionAsync(Project myNewProject, string oldNameBase, string newFileName, bool isCaseSensitive)
        {
            Project project = myNewProject;
            Solution renamedSolution = null;

            while (project != null)
            {
                Document newDocument = GetDocument(project, newFileName);
                if (newDocument == null)
                    return renamedSolution;

                SyntaxNode root = await GetRootNode(newDocument);
                if (root == null)
                    return renamedSolution;

                SemanticModel semanticModel = await newDocument.GetSemanticModelAsync();
                if (semanticModel == null)
                    return renamedSolution;

                IEnumerable<SyntaxNode> declarations = root.DescendantNodes().Where(n => HasMatchingSyntaxNode(semanticModel, n, oldNameBase, isCaseSensitive));
                SyntaxNode declaration = declarations.FirstOrDefault();
                if (declaration == null)
                    return renamedSolution;

                bool userConfirmed = await CheckUserConfirmation(oldNameBase);
                if (!userConfirmed)
                    return renamedSolution;

                string newName = Path.GetFileNameWithoutExtension(newDocument.FilePath);

                // Note that RenameSymbolAsync will return a new snapshot of solution.
                renamedSolution = await _roslynServices.RenameSymbolAsync(newDocument.Project.Solution, semanticModel.GetDeclaredSymbol(declaration), newName);
                project = renamedSolution.Projects.Where(p => StringComparers.Paths.Equals(p.FilePath, myNewProject.FilePath)).FirstOrDefault();
            }
            return null;
        }

    }
}
