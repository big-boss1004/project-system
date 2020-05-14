﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    public class LaunchTargetPropertyPageEnumProviderTests
    {
        [Fact]
        public async Task GetProviderAsync_ReturnsNonNullGenerator()
        {
            var project = ConfiguredProjectFactory.Create();

            var provider = new LaunchTargetPropertyPageEnumProvider(project);
            var generator = await provider.GetProviderAsync(options: null);

            Assert.NotNull(generator);
        }

        [Fact]
        public async Task TryCreateEnumValueAsync_Throws()
        {
            var project = ConfiguredProjectFactory.Create();

            var provider = new LaunchTargetPropertyPageEnumProvider(project);
            var generator = await provider.GetProviderAsync(options: null);

            await Assert.ThrowsAsync<NotImplementedException>(() => generator.TryCreateEnumValueAsync("MyTarget"));
        }

        [Fact]
        public async Task GetListValuesAsync_ReturnsPageNamesAndDisplayNames()
        {
            var catalogProvider = GetCatalogProviderAndData();

            var provider = new LaunchTargetPropertyPageEnumProvider(
                ConfiguredProjectFactory.Create(
                    services: ConfiguredProjectServicesFactory.Create(
                        propertyPagesCatalogProvider: catalogProvider)));
            var generator = await provider.GetProviderAsync(options: null);

            var values = await generator.GetListedValuesAsync();

            Assert.Collection(values, new Action<IEnumValue>[]
            {
                ev => { Assert.Equal(expected: "BetaCommandPageId", actual: ev.Name); Assert.Equal(expected: "Beta", actual: ev.DisplayName); },
                ev => { Assert.Equal(expected: "GammaCommandPageId", actual: ev.Name); Assert.Equal(expected: "Gamma", actual: ev.DisplayName); }
            });
        }

        private static IPropertyPagesCatalogProvider GetCatalogProviderAndData()
        {
            var betaPage = IRuleFactory.Create(
                name: "BetaCommandPageId",
                displayName: "Beta",
                pageTemplate: "Debugger",
                metadata: new Dictionary<string, object>
                {
                    { "CommandName", "BetaCommand" }
                });
            var gammaPage = IRuleFactory.Create(
                name: "GammaCommandPageId",
                displayName: "Gamma",
                pageTemplate: "Debugger",
                metadata: new Dictionary<string, object>
                {
                    { "CommandName", "GammaCommand" }
                });

            var catalog = IPropertyPagesCatalogFactory.Create(
                new Dictionary<string, IRule>
                {
                    { "BetaPage", betaPage },
                    { "GammaPage", gammaPage }
                });

            var catalogProvider = IPropertyPagesCatalogProviderFactory.Create(
                new Dictionary<string, IPropertyPagesCatalog> { { "Project", catalog } });

            return catalogProvider;
        }
    }
}
