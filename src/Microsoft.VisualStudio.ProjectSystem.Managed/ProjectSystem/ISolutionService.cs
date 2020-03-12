﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    /// A global service that tracks whether solution-level state.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.OneOrZero)]
    internal interface ISolutionService
    {
        /// <summary>
        /// Gets whether the solution is being closed, which can be useful to avoid doing
        /// redundant work while tearing down the solution.
        /// </summary>
        bool IsSolutionClosing { get; }
    }
}
