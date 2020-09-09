﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class UnconfiguredProjectServicesFactory
    {
        public static UnconfiguredProjectServices Create(IProjectThreadingService? threadingService = null, IProjectFaultHandlerService? projectFaultHandlerService = null,
            IProjectService? projectService = null)
        {
            projectFaultHandlerService ??= IProjectFaultHandlerServiceFactory.Create();
            threadingService ??= IProjectThreadingServiceFactory.Create();

            var projectLockService = IProjectLockServiceFactory.Create();

            var mock = new Mock<UnconfiguredProjectServices>();

            projectService ??= IProjectServiceFactory.Create(ProjectServicesFactory.Create(threadingService, projectLockService: projectLockService));

            mock.SetupGet(p => p.ProjectService)
                .Returns(projectService);

            mock.Setup(p => p.ProjectLockService)
                .Returns(projectLockService);

            mock.Setup(p => p.FaultHandler)
                .Returns(projectFaultHandlerService);

            return mock.Object;
        }
    }
}
