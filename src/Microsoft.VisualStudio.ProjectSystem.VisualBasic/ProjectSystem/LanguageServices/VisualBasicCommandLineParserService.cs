﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;

using Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    [Export(typeof(ICommandLineParserService))]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal class VisualBasicCommandLineParserService : ICommandLineParserService
    {
        public BuildOptions Parse(IEnumerable<string> arguments, string baseDirectory)
        {
            Requires.NotNull(arguments, nameof(arguments));

            return BuildOptions.FromCommandLineArguments(
                VisualBasicCommandLineParser.Default.Parse(arguments, baseDirectory, sdkDirectory: null, additionalReferenceDirectories: null));
        }
    }
}
