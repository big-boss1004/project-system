﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Query;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    public static class IPropertyPageQueryCacheFactory
    {
        internal static IPropertyPageQueryCache Create(
            IImmutableSet<ProjectConfiguration>? projectConfigurations = null,
            Func<ProjectConfiguration, string, IRule>? bindToRule = null)
        {
            var mock = new Mock<IPropertyPageQueryCache>();

            if (projectConfigurations is not null)
            {
                mock.Setup(cache => cache.GetKnownConfigurationsAsync()).ReturnsAsync(projectConfigurations);
            }

            if (bindToRule is not null)
            {
                mock.Setup(cache => cache
                    .BindToRule(It.IsAny<ProjectConfiguration>(), It.IsAny<string>()))
                    .Returns((ProjectConfiguration config, string schema) => Task.FromResult<IRule?>(bindToRule(config, schema)));
            }

            return mock.Object;
        }
    }

    public static class IPropertyPageQueryCacheProviderFactory
    {
        internal static IPropertyPageQueryCacheProvider Create()
        {
            var cache = IPropertyPageQueryCacheFactory.Create();
            return Create(cache);
        }

        internal static IPropertyPageQueryCacheProvider Create(IPropertyPageQueryCache cache)
        {
            var mock = new Mock<IPropertyPageQueryCacheProvider>();
            mock.Setup(f => f.CreateCache(It.IsAny<UnconfiguredProject>())).Returns(cache);

            return mock.Object;
        }
    }
}
