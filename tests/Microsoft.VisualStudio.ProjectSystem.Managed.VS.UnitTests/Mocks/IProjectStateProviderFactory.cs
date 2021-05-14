﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.Query;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    public static class IProjectStateProviderFactory
    {
        internal static IProjectStateProvider Create()
        {
            var cache = IProjectStateFactory.Create();
            return Create(cache);
        }

        internal static IProjectStateProvider Create(IProjectState cache)
        {
            var mock = new Mock<IProjectStateProvider>();
            mock.Setup(f => f.CreateState(It.IsAny<UnconfiguredProject>())).Returns(cache);

            return mock.Object;
        }
    }
}
