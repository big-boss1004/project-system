﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IUserNotificationServices
    {
        /// <summary>
        ///     Shows a Yes/No confirmation box to the user.
        /// </summary>
        /// <returns>
        ///     <see langword="true"/> if the user clicked the Yes button, otherwise; 
        ///     <see langword="false"/> if the user clicked the No button.
        /// </returns>
        ///<exception cref="COMException">
        ///     This method was not accessed from the UI thread.
        /// </exception>
        bool Confirm(string message);

        /// <summary>
        ///     Shows a warning to the user.
        /// </summary>
        ///<exception cref="COMException">
        ///     This method was not accessed from the UI thread.
        /// </exception>
        void ShowWarning(string warning);

        /// <summary>
        ///     Shows a error to the user.
        /// </summary>
        ///<exception cref="COMException">
        ///     This method was not accessed from the UI thread.
        /// </exception>
        void ShowError(string error);
    }
}
