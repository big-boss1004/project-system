﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [Trait("UnitTest", "ProjectSystem")]
    public class ImplicitProjectPropertiesProviderTests
    {
        [Fact]
        public void Constructor_NullDelegatedProvider_ThrowsArgumentNullException()
        {
            var project = UnconfiguredProjectFactory.Create();
            Assert.Throws<ArgumentNullException>("provider", () =>
            {
                new ImplicitProjectPropertiesProvider(null,
                                                      IProjectInstancePropertiesProviderFactory.Create(),
                                                      new ImplicitProjectPropertiesStore<string, string>(project),
                                                      project);
            });
        }

        [Fact]
        public void Constructor_NullDelegatedInstanceProvider_ThrowsArgumentNullException()
        {
            var delegatePropertiesMock = IProjectPropertiesFactory
                .MockWithPropertyAndValue("ProjectGuid", "7259e9ef-87d1-45a5-95c6-3a8432d23776");

            var delegateProperties = delegatePropertiesMock.Object;
            var delegateProvider = IProjectPropertiesProviderFactory.Create(delegateProperties);
            var project = UnconfiguredProjectFactory.Create();

            Assert.Throws<ArgumentNullException>("instanceProvider", () =>
            {
                new ImplicitProjectPropertiesProvider(delegateProvider,
                                                      null,
                                                      new ImplicitProjectPropertiesStore<string, string>(project),
                                                      project);
            });
        }

        [Fact]
        public void Constructor_NullUnconfiguredProject_ThrowsArgumentNullException()
        {
            var delegatePropertiesMock = IProjectPropertiesFactory
                .MockWithPropertyAndValue("ProjectGuid", "7259e9ef-87d1-45a5-95c6-3a8432d23776");

            var delegateProperties = delegatePropertiesMock.Object;
            var delegateProvider = IProjectPropertiesProviderFactory.Create(delegateProperties);

            Assert.Throws<ArgumentNullException>("project", () =>
            {
                new ImplicitProjectPropertiesProvider(delegateProvider,
                                                      IProjectInstancePropertiesProviderFactory.Create(),
                                                      new ImplicitProjectPropertiesStore<string, string>(UnconfiguredProjectFactory.Create()),
                                                      null);
            });
        }

        [Fact]
        public void Provider_SetsPropertyIfPresent()
        {
            var project = UnconfiguredProjectFactory.Create();
            var instanceProvider = IProjectInstancePropertiesProviderFactory.Create();

            var delegatePropertiesMock = IProjectPropertiesFactory
                .MockWithPropertyAndValue("ProjectGuid", "7259e9ef-87d1-45a5-95c6-3a8432d23776");

            var delegateProperties = delegatePropertiesMock.Object;
            var delegateProvider = IProjectPropertiesProviderFactory.Create(delegateProperties);
            var propertyStore = new ImplicitProjectPropertiesStore<string, string>(project);

            var provider = new ImplicitProjectPropertiesProvider(delegateProvider, instanceProvider, propertyStore, project);
            var properties = provider.GetProperties("path/to/project.testproj", null, null);

            // calls delegate above with matching values
            properties.SetPropertyValueAsync("ProjectGuid", "7259e9ef-87d1-45a5-95c6-3a8432d23776");

            // verify all the setups
            delegatePropertiesMock.Verify(p => p.SetPropertyValueAsync("ProjectGuid", "7259e9ef-87d1-45a5-95c6-3a8432d23776", null));
        }

        [Fact]
        public void Provider_IgnoresPropertyIfAbsent()
        {
            var project = UnconfiguredProjectFactory.Create();
            var instanceProvider = IProjectInstancePropertiesProviderFactory.Create();

            var delegatePropertiesMock = IProjectPropertiesFactory
                .MockWithProperty("SomeOtherProperty");

            var delegateProperties = delegatePropertiesMock.Object;
            var delegateProvider = IProjectPropertiesProviderFactory.Create(delegateProperties);
            var propertyStore = new ImplicitProjectPropertiesStore<string, string>(project);

            var provider = new ImplicitProjectPropertiesProvider(delegateProvider, instanceProvider, propertyStore, project);
            var properties = provider.GetProperties("path/to/project.testproj", null, null);

            // does not call the set property on the delegated property above
            properties.SetPropertyValueAsync("ProjectGuid", "7259e9ef-87d1-45a5-95c6-3a8432d23776");

            // verify all the setups
            delegatePropertiesMock.VerifyAll();
        }

        [Fact]
        public void Provider_ReturnsImplicitPropertyProvider()
        {
            var project = UnconfiguredProjectFactory.Create();
            var instanceProvider = IProjectInstancePropertiesProviderFactory.ImplementsGetItemTypeProperties();

            var delegatePropertiesMock = IProjectPropertiesFactory
                .MockWithPropertyAndValue("Something", "NotImportant");

            var delegateProperties = delegatePropertiesMock.Object;
            var delegateProvider = IProjectPropertiesProviderFactory.Create(delegateProperties);
            var propertyStore = new ImplicitProjectPropertiesStore<string, string>(project);

            var provider = new ImplicitProjectPropertiesProvider(delegateProvider, instanceProvider, propertyStore, project);
            var properties = provider.GetItemTypeProperties(null, "");

            properties.SetPropertyValueAsync("ProjectGuid", "7259e9ef-87d1-45a5-95c6-3a8432d23776");

            Assert.Equal("7259e9ef-87d1-45a5-95c6-3a8432d23776", properties.GetEvaluatedPropertyValueAsync("ProjectGuid").Result);
        }
    }
}
