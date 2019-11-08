﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.Xproj;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.Packaging
{
    [Guid(PackageGuid)]
    [PackageRegistration(AllowsBackgroundLoading = true, RegisterUsing = RegistrationMethod.CodeBase, UseManagedResourcesOnly = true)]
    [ProvideProjectFactory(typeof(XprojProjectFactory), null, "#27", "xproj", "xproj", null)]
    [ProvideAutoLoad(ActivationContextGuid, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideUIContextRule(ActivationContextGuid,
        name: "Load Managed Project Package",
        expression: "dotnetcore",
        termNames: new[] { "dotnetcore" },
        termValues: new[] { "SolutionHasProjectCapability:.NET & CPS" }
        )]
    [ProvideMenuResource("Menus.ctmenu", 4)]
    internal partial class ManagedProjectSystemPackage : AsyncPackage
    {
        public const string ActivationContextGuid = "E7DF1626-44DD-4E8C-A8A0-92EAB6DDC16E";
        public const string PackageGuid = "860A27C0-B665-47F3-BC12-637E16A1050A";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
#pragma warning disable RS0030 // Do not used banned APIs
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
#pragma warning restore RS0030 // Do not used banned APIs

            IComponentModel componentModel = await this.GetServiceAsync<SComponentModel, IComponentModel>();

            IEnumerable<Lazy<IPackageService>> packageServices = componentModel.DefaultExportProvider.GetExports<IPackageService>();

            foreach (Lazy<IPackageService> packageService in packageServices)
            {
                await packageService.Value.InitializeAsync(this);
            }

#if DEBUG
            DebuggerTraceListener.RegisterTraceListener();
#endif
        }
    }
}
