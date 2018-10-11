﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Hosts the "active" <see cref="IWorkspaceProjectContext"/> for a <see cref="UnconfiguredProject"/> 
    ///     and provides consumers access to it.
    /// </summary>
    internal interface IActiveWorkspaceProjectContextHost : IWorkspaceProjectContextHost
    {
    }
}
