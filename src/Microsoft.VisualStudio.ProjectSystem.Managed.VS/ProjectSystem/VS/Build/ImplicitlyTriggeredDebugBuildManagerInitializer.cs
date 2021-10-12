﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build
{
    /// <summary>
    /// Initializes the exported <see cref="ImplicitlyTriggeredDebugBuildManager"/> when the package loads.
    /// </summary>
    [Export(typeof(IPackageService))]
    internal sealed class ImplicitlyTriggeredDebugBuildManagerInitializer : IPackageService
    {
        private readonly IProjectServiceAccessor _projectServiceAccessor;

        [ImportingConstructor]
        public ImplicitlyTriggeredDebugBuildManagerInitializer(IProjectServiceAccessor projectServiceAccessor)
        {
            _projectServiceAccessor = projectServiceAccessor;
        }

        public Task InitializeAsync(IAsyncServiceProvider asyncServiceProvider)
        {
            ImplicitlyTriggeredDebugBuildManager buildManager = _projectServiceAccessor
                .GetProjectService()
                .Services
                .ExportProvider
                .GetExport<ImplicitlyTriggeredDebugBuildManager>()
                .Value;

            return buildManager.InitializeAsync();
        }
    }
}
