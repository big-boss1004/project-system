﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.VisualStudio.ProjectSystem.Logging;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal interface IProjectUpdatedHandler : IWorkspaceContextHandler
    {
        /// <summary>
        ///     Gets the project evaluation rule that the <see cref="IProjectEvaluationHandler"/> handles.
        /// </summary>
        string ProjectEvaluationRule { get; }

        void HandleProjectUpdate(IComparable version, IProjectChangeDescription projectChange, IProjectLogger logger);
    }
}
