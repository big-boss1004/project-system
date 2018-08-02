﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.ProjectSystem.Logging;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    public partial class AbstractEvaluationCommandLineHandlerTests
    {
        private class EvaluationCommandLineHandler : AbstractEvaluationCommandLineHandler
        {
            public EvaluationCommandLineHandler(UnconfiguredProject project) 
                : base(project)
            {
                FileNames = new Collection<string>();
            }

            public Collection<string> FileNames
            {
                get;
            }

            protected override void AddToContext(string fullPath, IImmutableDictionary<string, string> metadata, bool isActiveContext, IProjectLogger logger)
            {
                FileNames.Add(fullPath);
            }

            protected override void RemoveFromContext(string fullPath, IProjectLogger logger)
            {
                FileNames.Remove(fullPath);
            }
        }
    }
}
