﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models
{
    internal interface IDependenciesViewModelFactory
    {
        /// <summary>
        /// Returns a view model for a node that represents a target framework.
        /// </summary>
        IDependencyViewModel CreateTargetViewModel(ITargetFramework targetFramework, bool hasVisibleUnresolvedDependency);

        /// <summary>
        /// Returns a view model for a node that groups dependencies from a given provider.
        /// </summary>
        IDependencyViewModel? CreateGroupNodeViewModel(string providerType, bool hasVisibleUnresolvedDependency);

        /// <summary>
        /// Returns the icon to use for the "Dependencies" root node.
        /// </summary>
        ImageMoniker GetDependenciesRootIcon(bool hasVisibleUnresolvedDependency);
    }
}
