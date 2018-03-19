﻿using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    /// <summary>
    /// These are helper functions to select items, expand and collapse folders in a IVsUIHierarchy.
    /// This is hack due to CPS not exposing fuctionality to do this. They have it internally though.
    /// </summary>
    internal static class HACK_NodeHelper
    {
        /// <summary>
        /// Select an item in a IVsIHierarchy.
        /// Calls on the UI thread.
        /// </summary>
        public static async Task SelectAsync(ConfiguredProject configuredProject, IServiceProvider serviceProvider, IProjectTree node)
        {
            Requires.NotNull(configuredProject, nameof(configuredProject));
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(node, nameof(node));

            await configuredProject.Services.ProjectService.Services.ThreadingPolicy.SwitchToUIThread();

            Select(configuredProject, serviceProvider, (uint)node.Identity.ToInt32());

            await TaskScheduler.Default;
        }

        /// <summary>
        /// Expand a folder in a IVsUIHierarchy.
        /// </summary>
        public static async Task ExpandFolderAsync(ConfiguredProject configuredProject, IServiceProvider serviceProvider, IProjectTree node)
        {
            Requires.NotNull(configuredProject, nameof(configuredProject));
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(node, nameof(node));

            await configuredProject.Services.ProjectService.Services.ThreadingPolicy.SwitchToUIThread();

            ExpandFolder(configuredProject, serviceProvider, (uint)node.Identity.ToInt32());

            await TaskScheduler.Default;
        }

        /// <summary>
        /// Collapse a folder in a IVsUIHierarchy.
        /// </summary>
        public static async Task CollapseFolderAsync(ConfiguredProject configuredProject, IServiceProvider serviceProvider, IProjectTree node)
        {
            Requires.NotNull(configuredProject, nameof(configuredProject));
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(node, nameof(node));

            await configuredProject.Services.ProjectService.Services.ThreadingPolicy.SwitchToUIThread();

            CollapseFolder(configuredProject, serviceProvider, (uint)node.Identity.ToInt32());

            await TaskScheduler.Default;
        }

        /// <summary>
        /// Select an item in a IVsIHierarchy.
        /// </summary>
        private static void Select(ConfiguredProject configuredProject, IServiceProvider serviceProvider, uint itemId)
        {
            UseWindow(configuredProject, serviceProvider, (hierarchy, window) =>
            {
                // We need to unselect the item if it is already selected to re-select it correctly.
                window.ExpandItem(hierarchy, itemId, EXPANDFLAGS.EXPF_UnSelectItem);
                window.ExpandItem(hierarchy, itemId, EXPANDFLAGS.EXPF_SelectItem);
            });
        }

        /// <summary>
        /// Expand a folder in a IVsUIHierarchy.
        /// </summary>
        private static void ExpandFolder(ConfiguredProject configuredProject, IServiceProvider serviceProvider, uint itemId)
        {
            UseWindow(configuredProject, serviceProvider, (hierarchy, window) =>
            {
                window.ExpandItem(hierarchy, itemId, EXPANDFLAGS.EXPF_ExpandFolder);
            });
        }

        /// <summary>
        /// Collapse a folder in a IVsUIHierarchy.
        /// </summary>
        private static void CollapseFolder(ConfiguredProject configuredProject, IServiceProvider serviceProvider, uint itemId)
        {
            UseWindow(configuredProject, serviceProvider, (hierarchy, window) =>
            {
                window.ExpandItem(hierarchy, itemId, EXPANDFLAGS.EXPF_CollapseFolder);
            });
        }

        /// <summary>
        /// Callbacks with a hierarchy and hierarchy window for use.
        /// </summary>
        private static void UseWindow(ConfiguredProject configuredProject, IServiceProvider serviceProvider, Action<IVsUIHierarchy, IVsUIHierarchyWindow> callback)
        {
            var hierarchy = (IVsUIHierarchy)configuredProject.UnconfiguredProject.Services.HostObject;
            callback(hierarchy, GetUIHierarchyWindow(serviceProvider, VSConstants.StandardToolWindows.SolutionExplorer));
        }

        /// <summary>
        /// Get reference to IVsUIHierarchyWindow interface from guid persistence slot.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="persistenceSlot">Unique identifier for a tool window created using IVsUIShell::CreateToolWindow.
        /// The caller of this method can use predefined identifiers that map to tool windows if those tool windows
        /// are known to the caller. </param>
        /// <returns>A reference to an IVsUIHierarchyWindow interface, or <c>null</c> if the window isn't available, such as command line mode.</returns>
        private static IVsUIHierarchyWindow GetUIHierarchyWindow(IServiceProvider serviceProvider, Guid persistenceSlot)
        {
            Requires.NotNull(serviceProvider, nameof(serviceProvider));

            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }


            var shell = (IVsUIShell)serviceProvider.GetService<SVsUIShell>();

            object pvar = null;
            IVsUIHierarchyWindow uiHierarchyWindow = null;

            try
            {
                if (ErrorHandler.Succeeded(shell.FindToolWindow(0, ref persistenceSlot, out var frame)) && frame != null)
                {
                    ErrorHandler.ThrowOnFailure(frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out pvar));
                }
            }
            finally
            {
                if (pvar != null)
                {
                    uiHierarchyWindow = (IVsUIHierarchyWindow)pvar;
                }
            }

            return uiHierarchyWindow;
        }
    }
}
