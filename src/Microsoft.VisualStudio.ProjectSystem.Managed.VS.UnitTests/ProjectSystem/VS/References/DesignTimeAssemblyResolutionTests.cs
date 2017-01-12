﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using VSLangProj;
using VSLangProj80;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    public class DesignTimeAssemblyResolutionTests
    {
        [Fact]
        public void GetTargetFramework_WhenUnderlyingGetPropertyReturnsHResult_ReturnsHResult()
        {
            var hierarchy = IVsHierarchyFactory.ImplementGetProperty(VSConstants.E_INVALIDARG);
            var resolution = CreateInstance(hierarchy);

            var result = resolution.GetTargetFramework(out string _);

            Assert.Equal(result, VSConstants.E_INVALIDARG);
        }

        [Fact]
        public void GetTargetFramework_WhenUnderlyingGetPropertyReturnsHResult_SetsTargetFrameworkToNull()
        {
            var hierarchy = IVsHierarchyFactory.ImplementGetProperty(VSConstants.E_INVALIDARG);
            var resolution = CreateInstance(hierarchy);

            resolution.GetTargetFramework(out string result);

            Assert.Null(result);
        }

        [Fact]
        public void GetTargetFramework_WhenUnderlyingGetPropertyReturnsDISP_E_MEMBERNOTFOUND_ReturnsOK()
        {
            var hierarchy = IVsHierarchyFactory.ImplementGetProperty(VSConstants.DISP_E_MEMBERNOTFOUND);
            var resolution = CreateInstance(hierarchy);

            var result = resolution.GetTargetFramework(out string _);

            Assert.Equal(VSConstants.S_OK, result);
        }

        [Fact]
        public void GetTargetFramework_WhenUnderlyingGetPropertyReturnsDISP_E_MEMBERNOTFOUND_SetsTargetFrameworkToNull()
        {
            var hierarchy = IVsHierarchyFactory.ImplementGetProperty(VSConstants.DISP_E_MEMBERNOTFOUND);
            var resolution = CreateInstance(hierarchy);

            resolution.GetTargetFramework(out string result);

            Assert.Null(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData(".NETFramework, Version=v4.5")]
        [InlineData(".NETFramework, Version=v4.5, Profile=Client")]
        public void GetTargetFramework_WhenUnderlyingGetPropertyReturnsValue_SetsTargetFramework(string input)
        {
            var hierarchy = IVsHierarchyFactory.ImplementGetProperty(input);
            var resolution = CreateInstance(hierarchy);

            var hr = resolution.GetTargetFramework(out string result);

            Assert.Equal(input, result);
            Assert.Equal(VSConstants.S_OK, hr);
        }

        [Fact]
        public void ResolveAssemblyPathInTargetFx_NullAsAssemblySpecs_ReturnsE_INVALIDARG()
        {
            var resolution = CreateInstance();

            var result = resolution.ResolveAssemblyPathInTargetFx((string[])null, 1, new VsResolvedAssemblyPath[1], out uint resolvedAssemblyPaths);

            Assert.Equal(VSConstants.E_INVALIDARG, result);
            Assert.Equal(0u, resolvedAssemblyPaths);
        }

        [Fact]
        public void ResolveAssemblyPathInTargetFx_ZeroAsAssembliesToResolve_ReturnsE_INVALIDARG()
        {
            var resolution = CreateInstance();

            var result = resolution.ResolveAssemblyPathInTargetFx(new string[] { "mscorlib" }, 0, new VsResolvedAssemblyPath[1], out uint resolvedAssemblyPaths);

            Assert.Equal(VSConstants.E_INVALIDARG, result);
            Assert.Equal(0u, resolvedAssemblyPaths);
        }

        [Fact]
        public void ResolveAssemblyPathInTargetFx_NullAsResolvedAssemblyPaths_ReturnsE_INVALIDARG()
        {
            var resolution = CreateInstance();

            var result = resolution.ResolveAssemblyPathInTargetFx(new string[] { "mscorlib" }, 1, (VsResolvedAssemblyPath[])null, out uint resolvedAssemblyPaths);

            Assert.Equal(VSConstants.E_INVALIDARG, result);
            Assert.Equal(0u, resolvedAssemblyPaths);
        }

        [Fact]
        public void ResolveAssemblyPathInTargetFx_MoreAssemblySpecsThanAssembliesToResolve_ReturnsE_INVALIDARG()
        {
            var resolution = CreateInstance();

            var result = resolution.ResolveAssemblyPathInTargetFx(new string[] { "mscorlib", "System" }, 1, new VsResolvedAssemblyPath[2], out uint resolvedAssemblyPaths);

            Assert.Equal(VSConstants.E_INVALIDARG, result);
            Assert.Equal(0u, resolvedAssemblyPaths);
        }

        [Fact]
        public void ResolveAssemblyPathInTargetFx_MoreAssemblySpecsThanResolvedAssemblyPaths_ReturnsE_INVALIDARG()
        {
            var resolution = CreateInstance();

            var result = resolution.ResolveAssemblyPathInTargetFx(new string[] { "mscorlib", "System" }, 2, new VsResolvedAssemblyPath[1], out uint resolvedAssemblyPaths);

            Assert.Equal(VSConstants.E_INVALIDARG, result);
            Assert.Equal(0u, resolvedAssemblyPaths);
        }


        [Fact]
        public void ResolveAssemblyPathInTargetFx_MoreAssembliesToResolveThanAssemblySpecs_ReturnsE_INVALIDARG()
        {
            var resolution = CreateInstance();

            var result = resolution.ResolveAssemblyPathInTargetFx(new string[] { "mscorlib" }, 2, new VsResolvedAssemblyPath[1], out uint resolvedAssemblyPaths);

            Assert.Equal(VSConstants.E_INVALIDARG, result);
            Assert.Equal(0u, resolvedAssemblyPaths);
        }

        [Fact]
        public void ResolveAssemblyPathInTargetFx_MoreResolvedAssemblyPathsThanAssemblySpecs_ReturnsE_INVALIDARG()
        {
            var resolution = CreateInstance();

            var result = resolution.ResolveAssemblyPathInTargetFx(new string[] { "mscorlib" }, 1, new VsResolvedAssemblyPath[2], out uint resolvedAssemblyPaths);

            Assert.Equal(VSConstants.E_INVALIDARG, result);
            Assert.Equal(0u, resolvedAssemblyPaths);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("System, Bar")]
        [InlineData("System, Version=NotAVersion")]
        [InlineData("System, PublicKeyToken=ABC")]
        public void ResolveAssemblyPathInTargetFx_InvalidNameAsAssemblySpec_ReturnsE_INVALIDARG(string input)
        {
            var resolution = CreateInstance();

            var result = resolution.ResolveAssemblyPathInTargetFx(new string[] { input }, 1, new VsResolvedAssemblyPath[1], out uint resolvedAssemblyPaths);

            Assert.Equal(VSConstants.E_INVALIDARG, result);
            Assert.Equal(0u, resolvedAssemblyPaths);
        }

        [Theory]    // Input                                                                        // Name             // Version          // Path
        [InlineData("System",                                                                       "System",           "",                 @"C:\System.dll")]
        [InlineData("System",                                                                       "System",           "1",                @"C:\System.dll")]
        [InlineData("System",                                                                       "System",           "1.0",              @"C:\System.dll")]
        [InlineData("System",                                                                       "System",           "1.0.0",            @"C:\System.dll")]
        [InlineData("System",                                                                       "System",           "1.0.0.0",          @"C:\System.dll")]
        [InlineData("System.Foo",                                                                   "System.Foo",       "1.0.0.0",          @"C:\System.Foo.dll")]
        [InlineData("System, Version=1.0.0.0",                                                      "System",           "1.0.0.0",          @"C:\System.dll")]
        [InlineData("System, Version=1.0.0.0",                                                      "System",           "2.0.0.0",          @"C:\System.dll")]      // We let a later version satisfy an earlier version
        [InlineData("System, Version=1.0",                                                          "System",           "2.0.0.0",          @"C:\System.dll")]
        [InlineData("System, Version=1.0",                                                          "System",           "2.0.0.0",          @"C:\System.dll")]
        [InlineData("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",    "System",           "4.0.0.0",          @"C:\System.dll")]
        public void ResolveAssemblyPathInTargetFx_NameThatMatches_ReturnsResolvedPaths(string input, string name, string version, string path)
        {
            var reference = Reference3Factory.CreateAssemblyReference(name, version, path);

            var resolution = CreateInstance(reference);

            var resolvedPaths = new VsResolvedAssemblyPath[1];
            var result = resolution.ResolveAssemblyPathInTargetFx(new string[] { input }, 1, resolvedPaths, out uint resolvedAssemblyPaths);

            Assert.Equal(VSConstants.S_OK, result);
            Assert.Equal(1u, resolvedAssemblyPaths);
            Assert.Equal(input, resolvedPaths[0].bstrOrigAssemblySpec);
            Assert.Equal(path, resolvedPaths[0].bstrResolvedAssemblyPath);
        }

        [Theory]    // Input                                                                        // Name             // Version          // Path
        [InlineData("System",                                                                       "System.Core",      "",                 @"C:\System.Core.dll")]
        [InlineData("System",                                                                       "system",           "",                 @"C:\System.dll")]
        [InlineData("system",                                                                       "System",           "",                 @"C:\System.dll")]
        [InlineData("encyclopædia",                                                                 "encyclopaedia",    "",                 @"C:\System.dll")]
        [InlineData("System, Version=1.0.0.0",                                                      "System",           "",                 @"C:\System.dll")]
        [InlineData("System, Version=2.0.0.0",                                                      "System",           "1.0.0.0",          @"C:\System.dll")]
        public void ResolveAssemblyPathInTargetFx_NameThatDoesNotMatch_SetsResolvedAssemblysToZero(string input, string name, string version, string path)
        {
            var reference = Reference3Factory.CreateAssemblyReference(name, version, path);

            var resolution = CreateInstance(reference);

            var resolvedPaths = new VsResolvedAssemblyPath[1];
            var result = resolution.ResolveAssemblyPathInTargetFx(new string[] { input }, 1, resolvedPaths, out uint resolvedAssemblyPaths);

            Assert.Equal(VSConstants.S_OK, result);
            Assert.Equal(0u, resolvedAssemblyPaths);
            Assert.Null(resolvedPaths[0].bstrOrigAssemblySpec);
            Assert.Null(resolvedPaths[0].bstrResolvedAssemblyPath);
        }

        private static DesignTimeAssemblyResolution CreateInstance(params Reference[] references)
        {
            VSProject vsProject = VSProjectFactory.ImplementReferences(references);
            Project project = ProjectFactory.ImplementObject(() => vsProject);
            IVsHierarchy hierarchy = IVsHierarchyFactory.ImplementGetProperty(project);

            return CreateInstance(hierarchy);
        }

        private static DesignTimeAssemblyResolution CreateInstance(IVsHierarchy hierarchy = null)
        {
            hierarchy = hierarchy ?? IVsHierarchyFactory.Create();

            IUnconfiguredProjectVsServices projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(() => hierarchy);

            return new DesignTimeAssemblyResolution(projectVsServices);
        }
    }
}