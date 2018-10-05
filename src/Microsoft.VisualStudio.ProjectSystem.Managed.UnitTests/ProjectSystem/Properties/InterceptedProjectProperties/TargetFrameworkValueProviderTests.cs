﻿using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.Properties;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.ProjectPropertiesProviders
{
    public class TargetFrameworkValueProviderTests
    {
        private const string TargetFrameworkPropertyName = "TargetFramework";

        private InterceptedProjectPropertiesProviderBase CreateInstance(FrameworkName configuredTargetFramework)
        {
            var data = new PropertyPageData()
            {
                Category = ConfigurationGeneral.SchemaName,
                PropertyName = ConfigurationGeneral.TargetFrameworkMonikerProperty,
                Value = configuredTargetFramework.FullName
            };

            var project = UnconfiguredProjectFactory.Create();
            var properties = ProjectPropertiesFactory.Create(project, data);
            var instanceProvider = IProjectInstancePropertiesProviderFactory.Create();

            var delegatePropertiesMock = IProjectPropertiesFactory
                .MockWithProperty(TargetFrameworkPropertyName);

            var delegateProperties = delegatePropertiesMock.Object;
            var delegateProvider = IProjectPropertiesProviderFactory.Create(delegateProperties);

            var targetFrameworkProvider = new TargetFrameworkValueProvider(properties);
            var providerMetadata = IInterceptingPropertyValueProviderMetadataFactory.Create(TargetFrameworkPropertyName);
            var lazyArray = new[] { new Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>(
                () => targetFrameworkProvider, providerMetadata) };
            return new ProjectFileInterceptedProjectPropertiesProvider(delegateProvider, instanceProvider, project, lazyArray);
        }

        [Fact]
        public async Task VerifyGetTargetFrameworkPropertyAsync()
        {
            var configuredTargetFramework = new FrameworkName(".NETFramework", new Version(4, 5));
            var expectedTargetFrameworkPropertyValue = (uint)0x40005;
            var provider = CreateInstance(configuredTargetFramework);
            var properties = provider.GetProperties("path/to/project.testproj", null, null);
            var propertyValueStr = await properties.GetEvaluatedPropertyValueAsync(TargetFrameworkPropertyName);
            Assert.True(uint.TryParse(propertyValueStr, out uint propertyValue));
            Assert.Equal(expectedTargetFrameworkPropertyValue, propertyValue);
        }
    }
}
