﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     A configured-project service which will be activated when its configured project becomes implicitly active, or deactivated when it not.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ZeroOrMore)]
    internal interface IImplicitlyActiveService
    {
        /// <summary>
        ///     Activates the service.
        /// </summary>
        Task ActivateAsync();

        /// <summary>
        ///     Deactivates the service.
        /// </summary>
        Task DeactivateAsync();
    }
}
