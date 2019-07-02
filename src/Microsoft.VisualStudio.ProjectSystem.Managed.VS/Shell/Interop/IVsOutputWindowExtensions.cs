﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Microsoft.VisualStudio.ProjectSystem.VS;

#nullable disable

namespace Microsoft.VisualStudio.Shell.Interop
{
    /// <summary>
    ///     Provides extension methods for <see cref="IVsOutputWindow"/>.
    /// </summary>
    internal static class IVsOutputWindowExtensions
    {
        /// <summary>
        ///     Actives the output window pane associated with the specified GUID. Does nothing if the pane cannot be found.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="outputWindow"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="paneGuid"/> is empty.
        /// </exception>
        public static void ActivatePane(this IVsOutputWindow outputWindow, Guid paneGuid)
        {
            Requires.NotNull(outputWindow, nameof(outputWindow));
            Requires.NotEmpty(paneGuid, nameof(paneGuid));

            HResult hr = outputWindow.GetPane(ref paneGuid, out IVsOutputWindowPane pane);
            if (hr.IsOK) // Pane found
            {
                hr = pane.Activate();
                if (hr.Failed)
                    throw hr.Exception;
            }
        }

        /// <summary>
        ///     Returns the GUID associated with the active window pane, or <see cref="Guid.Empty"/> if no 
        ///     active pane or the active pane is unknown.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="outputWindow"/> is <see langword="null"/>.
        /// </exception>
        public static Guid GetActivePane(this IVsOutputWindow outputWindow)
        {
            Requires.NotNull(outputWindow, nameof(outputWindow));

            if (outputWindow is IVsOutputWindow2 outputWindow2)
            {
                HResult hr = outputWindow2.GetActivePaneGUID(out Guid activePaneGuid);
                if (hr.Failed)
                    throw hr.Exception;

                return activePaneGuid;
            }

            return Guid.Empty;
        }
    }
}
