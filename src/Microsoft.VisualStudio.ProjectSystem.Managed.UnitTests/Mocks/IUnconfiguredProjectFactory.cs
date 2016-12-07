﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IUnconfiguredProjectFactory
    {
        /// <summary>
        /// Extension methods cannot be mocked directly, so use a derived interface
        /// that provides the extension method and is only used when creating the mock.
        /// </summary>
        internal interface UnconfiguredProjectEx : UnconfiguredProject
        {
            string MakeRooted(string path);
        }

        public static UnconfiguredProject Create(object hostObject = null, IEnumerable<string> capabilities = null, string filePath = null,
            IProjectConfigurationsService projectConfigurationsService = null, ConfiguredProject configuredProject = null,
            string projectRoot = null)
        {
            capabilities = capabilities ?? Enumerable.Empty<string>();

            var service = IProjectServiceFactory.Create();

            var unconfiguredProjectServices = new Mock<IUnconfiguredProjectServices>();
            unconfiguredProjectServices.Setup(u => u.HostObject)
                                       .Returns(hostObject);

            unconfiguredProjectServices.Setup(u => u.ProjectConfigurationsService)
                                       .Returns(projectConfigurationsService);

            var unconfiguredProject = new Mock<UnconfiguredProjectEx>();
            unconfiguredProject.Setup(u => u.ProjectService)
                               .Returns(service);

            unconfiguredProject.Setup(u => u.Services)
                               .Returns(unconfiguredProjectServices.Object);

            unconfiguredProject.SetupGet(u => u.FullPath)
                                .Returns(filePath);

            unconfiguredProject.Setup(u => u.GetSuggestedConfiguredProjectAsync()).Returns(Task.FromResult(configuredProject));

            if (projectRoot != null)
            {
                unconfiguredProject.Setup(u => u.MakeRooted(It.IsAny<string>()))
                    .Returns<string>(path => Path.Combine(projectRoot, path));
            }

            return unconfiguredProject.Object;
        }
    }
}
