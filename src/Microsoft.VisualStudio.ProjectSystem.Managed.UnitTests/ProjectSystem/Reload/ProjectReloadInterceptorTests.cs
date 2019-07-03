﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Xml;

using Microsoft.Build.Construction;
using Microsoft.VisualStudio.ProjectSystem.Configuration;

using Xunit;

using StringReader = System.IO.StringReader;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem
{
    public class ProjectReloadInterceptorTests
    {
        [Theory]
        [InlineData(@"
<Project>
  <PropertyGroup>
  </PropertyGroup>
</Project>
", @"
<Project>
  <PropertyGroup>
    <TargetFrameworks>net45</TargetFrameworks>
  </PropertyGroup>
</Project>
")]
        [InlineData(@"
<Project>
  <PropertyGroup>
    <TargetFrameworkIdentifier>.NETFramework</TargetFrameworkIdentifier>
    <TargetFrameworkVersion>v4.5</TargetFrameworkVersion>
  </PropertyGroup>
</Project>
", @"
<Project>
  <PropertyGroup>
    <TargetFrameworks>net45;netcoreapp2.2</TargetFrameworks>
  </PropertyGroup>
</Project>
")]
        [InlineData(@"
<Project>
  <PropertyGroup>
    <TargetFrameworks>net45;netcoreapp2.2</TargetFrameworks>
  </PropertyGroup>
</Project>
", @"
<Project>
  <PropertyGroup>
    <TargetFramework>net45</TargetFramework>
  </PropertyGroup>
</Project>
")]
        [InlineData(@"
<Project>
  <PropertyGroup>
    <TargetFramework>net45</TargetFramework>    
  </PropertyGroup>
</Project>
", @"
<Project>
  <PropertyGroup>
    <TargetFrameworks>net45</TargetFrameworks>
  </PropertyGroup>
</Project>
")]
        [InlineData(@"
<Project>
  <PropertyGroup>
    <TargetFramework>net45</TargetFramework>    
  </PropertyGroup>
</Project>
", @"
<Project>
  <PropertyGroup>
    <TargetFrameworks>net45;netcoreapp2.2</TargetFrameworks>
  </PropertyGroup>
</Project>
")]
        [InlineData(@"
<Project>
  <PropertyGroup>
    <TargetFrameworks>net45;netcoreapp2.2</TargetFrameworks>
  </PropertyGroup>
</Project>
", @"
<Project>
  <PropertyGroup>
    <TargetFrameworkIdentifier>.NETFramework</TargetFrameworkIdentifier>
    <TargetFrameworkVersion>v4.5</TargetFrameworkVersion>
  </PropertyGroup>
</Project>
")]
        public void InterceptProjectReload_WhenDimensionsChange_ReturnsNeedsForceReload(string oldProjectXml, string newProjectXml)
        {
            var oldProperties = CreateProperties(oldProjectXml);
            var newProperties = CreateProperties(newProjectXml);

            var instance = CreateInstance();

            var result = instance.InterceptProjectReload(oldProperties, newProperties);

            Assert.Equal(ProjectReloadResult.NeedsForceReload, result);
        }

        [Theory]
        [InlineData(@"
<Project>
  <PropertyGroup>
  </PropertyGroup>
</Project>
", @"
<Project>
  <PropertyGroup>
    <TargetFramework>net45</TargetFramework>
  </PropertyGroup>
</Project>
")]
        [InlineData(@"
<Project>
  <PropertyGroup>
    <TargetFrameworkIdentifier>.NETFramework</TargetFrameworkIdentifier>
    <TargetFrameworkVersion>v4.5</TargetFrameworkVersion>
  </PropertyGroup>
</Project>
", @"
<Project>
  <PropertyGroup>
    <TargetFramework>net45</TargetFramework>
  </PropertyGroup>
</Project>
")]
        [InlineData(@"
<Project>
  <PropertyGroup>
    <TargetFramework>net45</TargetFramework>
  </PropertyGroup>
</Project>
", @"
<Project>
  <PropertyGroup>
    <TargetFramework>net45</TargetFramework>
  </PropertyGroup>
</Project>
")]
        [InlineData(@"
<Project>
  <PropertyGroup>
    <TargetFrameworks>net45;netcoreapp2.2</TargetFrameworks>
  </PropertyGroup>
</Project>
", @"
<Project>
  <PropertyGroup>
    <TargetFrameworks>net45;netcoreapp2.2</TargetFrameworks>
  </PropertyGroup>
</Project>
")]
        public void InterceptProjectReload_WhenNoDimensionsChange_ReturnsNoAction(string oldProjectXml, string newProjectXml)
        {
            var oldProperties = CreateProperties(oldProjectXml);
            var newProperties = CreateProperties(newProjectXml);

            var instance = CreateInstance();

            var result = instance.InterceptProjectReload(oldProperties, newProperties);

            Assert.Equal(ProjectReloadResult.NoAction, result);
        }

        private static ImmutableArray<ProjectPropertyElement> CreateProperties(string projectXml)
        {
            using var reader = XmlReader.Create(new StringReader(projectXml));

            return ProjectRootElement.Create(reader).Properties.ToImmutableArray();
        }

        private static ProjectReloadInterceptor CreateInstance()
        {
            var accessor = IProjectAccessorFactory.Create();
            var provider = new TargetFrameworkProjectConfigurationDimensionProvider(accessor);

            var project = UnconfiguredProjectFactory.Create();
            var instance = new ProjectReloadInterceptor(project);

            instance.DimensionProviders.Add(provider);

            return instance;
        }
    }
}
