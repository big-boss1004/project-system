﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.Telemetry
{
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IPackageRestoreTelemetryService
    {
        /// <summary>
        /// Posts a telemetry events from package restore components.
        /// </summary>
        /// <param name="packageRestoreOperationName">The name of the specific package restore operation.</param>
        /// <param name="fullPath">The full path to the project that needs a package restore.</param>
        void PostPackageRestoreEvent(string packageRestoreOperationName, string fullPath);

        /// <summary>
        /// Posts a telemetry events from package restore components including whether package restore is up to date.
        /// </summary>
        /// <param name="packageRestoreOperationName">The name of the specific package restore operation.</param>
        /// <param name="fullPath">The full path to the project that needs a package restore.</param>
        /// <param name="isRestoreUpToDate">Flag indicating whether the restore is up to date.</param>
        void PostPackageRestoreEvent(string packageRestoreOperationName, string fullPath, bool isRestoreUpToDate);
    }
}
