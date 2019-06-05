﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Shell.Flavor;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    [Guid(ProjectType.LegacyXProj)]
    internal sealed class MigrateXprojProjectFactory : FlavoredProjectFactoryBase, IVsProjectUpgradeViaFactory, IVsProjectUpgradeViaFactory4
    {
        public int UpgradeProject(
            string xprojLocation,
            uint upgradeFlags,
            string backupDirectory,
            out string migratedProjectFileLocation,
            IVsUpgradeLogger logger,
            out int upgradeRequired,
            out Guid migratedProjectGuid)
        {
            migratedProjectFileLocation = default;
            upgradeRequired = default;
            migratedProjectGuid = default;
            return VSConstants.S_FALSE;
        }

        public int UpgradeProject_CheckOnly(
            string fileName,
            IVsUpgradeLogger logger,
            out int upgradeRequired,
            out Guid migratedProjectFactory,
            out uint upgradeProjectCapabilityFlags)
        {
            bool isXproj = fileName.EndsWith(".xproj");

            // If the project is an xproj, then indicate it is deprecated. If it isn't, then there's nothing we can do with it.
            upgradeRequired = isXproj
                ? (int)__VSPPROJECTUPGRADEVIAFACTORYREPAIRFLAGS.VSPUVF_PROJECT_DEPRECATED
                : (int)__VSPPROJECTUPGRADEVIAFACTORYREPAIRFLAGS.VSPUVF_PROJECT_NOREPAIR;

            migratedProjectFactory = GetType().GUID;
            upgradeProjectCapabilityFlags = 0;
            return HResult.OK;
        }

        public void UpgradeProject_CheckOnly(
            string fileName,
            IVsUpgradeLogger logger,
            out uint upgradeRequired,
            out Guid migratedProjectFactory,
            out uint upgradeProjectCapabilityFlags)
        {
            UpgradeProject_CheckOnly(fileName, logger, out int iUpgradeRequired, out migratedProjectFactory, out upgradeProjectCapabilityFlags);
            upgradeRequired = unchecked((uint)iUpgradeRequired);
        }

        protected override object PreCreateForOuter(IntPtr outerProjectIUnknown)
        {
            // Should not be called
            throw new NotImplementedException();
        }

        public int GetSccInfo(string bstrProjectFileName, out string pbstrSccProjectName, out string pbstrSccAuxPath, out string pbstrSccLocalPath, out string pbstrProvider)
        {
            // Should not be called
            throw new NotImplementedException();
        }
    }
}
