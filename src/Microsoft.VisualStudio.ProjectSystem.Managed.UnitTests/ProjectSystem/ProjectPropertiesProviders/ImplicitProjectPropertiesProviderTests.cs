﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.ProjectPropertiesProviders
{
    [ProjectSystemTrait]
    public class ImplicitProjectPropertiesProviderTests
    {
        [Fact]
        public void Constructor_NullDelegatedProvider_ThrowsArgumentNullException()
        {            
            Assert.Throws<ArgumentNullException>("provider", () => 
            {
                new ImplicitProjectPropertiesProvider(null, IUnconfiguredProjectFactory.Create());
            });
        }

        [Fact]
        public void Constructor_NullUnconfiguredProject_ThrowsArgumentNullException()
        {
            var delegatePropertiesMock = IProjectPropertiesFactory
                .MockWithPropertyAndSet("ProjectGuid", "7259e9ef-87d1-45a5-95c6-3a8432d23776");

            var delegateProperties = delegatePropertiesMock.Object;
            var delegateProvider = IProjectPropertiesProviderFactory.Create(delegateProperties);

            Assert.Throws<ArgumentNullException>("unconfiguredProject", () =>
            {
                new ImplicitProjectPropertiesProvider(delegateProvider, null);
            });
        }

        [Fact]
        public void Provider_SetsPropertyIfPresent()
        {
            var unconfiguredProject = IUnconfiguredProjectFactory.Create();

            var delegatePropertiesMock = IProjectPropertiesFactory
                .MockWithPropertyAndSet("ProjectGuid", "7259e9ef-87d1-45a5-95c6-3a8432d23776");

            var delegateProperties = delegatePropertiesMock.Object;
            var delegateProvider = IProjectPropertiesProviderFactory.Create(delegateProperties);

            var provider = new ImplicitProjectPropertiesProvider(delegateProvider, unconfiguredProject);
            var properties = provider.GetProperties("path/to/project.testproj", null, null);

            // calls delegate above with matching values
            properties.SetPropertyValueAsync("ProjectGuid", "7259e9ef-87d1-45a5-95c6-3a8432d23776");

            // verify all the setups
            delegatePropertiesMock.VerifyAll();
        }

        [Fact]
        public void Provider_IgnoresPropertyIfAbsent()
        {
            var unconfiguredProject = IUnconfiguredProjectFactory.Create();

            var delegatePropertiesMock = IProjectPropertiesFactory
                .MockWithProperty("SomeOtherProperty");

            var delegateProperties = delegatePropertiesMock.Object;
            var delegateProvider = IProjectPropertiesProviderFactory.Create(delegateProperties);

            var provider = new ImplicitProjectPropertiesProvider(delegateProvider, unconfiguredProject);
            var properties = provider.GetProperties("path/to/project.testproj", null, null);

            // does not call the set property on the delegated property above
            properties.SetPropertyValueAsync("ProjectGuid", "7259e9ef-87d1-45a5-95c6-3a8432d23776");

            // verify all the setups
            delegatePropertiesMock.VerifyAll();
        }
    }
}
