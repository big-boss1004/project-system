﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Logging;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to the project and makes sure the language service is aware of them.
    /// </summary>
    internal class ProjectPropertiesItemHandler : IEvaluationHandler
    {
        private readonly IWorkspaceProjectContext _context;

        public ProjectPropertiesItemHandler(IWorkspaceProjectContext context)
        {
            Requires.NotNull(context, nameof(context));

            _context = context;
        }

        public void Handle(IComparable version, IProjectChangeDescription projectChange, bool isActiveContext, IProjectLogger logger)
        {
            Requires.NotNull(version, nameof(version));
            Requires.NotNull(projectChange, nameof(projectChange));
            Requires.NotNull(logger, nameof(logger));

            if (projectChange.Difference.ChangedProperties.Contains(ConfigurationGeneral.ProjectGuidProperty))
            {
                if (Guid.TryParse(projectChange.After.Properties[ConfigurationGeneral.ProjectGuidProperty], out Guid result))
                {
                    _context.Guid = result;
                }
            }

            // The language service wants both the intermediate (bin\obj) and output (bin\debug)) paths
            // so that it can automatically hook up project-to-project references. It does this by matching the 
            // bin output path with the another project's /reference argument, if they match, then it automatically 
            // introduces a project reference between the two. We pass the intermediate path via the /out 
            // command-line argument and set via one of the other handlers, where as the latter is calculated via 
            // the TargetPath property and explictly set on the context.

            if (projectChange.Difference.ChangedProperties.Contains(ConfigurationGeneral.TargetPathProperty))
            {
                var newBinOutputPath = projectChange.After.Properties[ConfigurationGeneral.TargetPathProperty];
                if (!string.IsNullOrEmpty(newBinOutputPath))
                {
                    _context.BinOutputPath = newBinOutputPath;
                }
            }
        }
    }
}
