﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.VS.Automation;

using Moq;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    public class DesignTimeInputsBuildManagerBridgeTests : IDisposable
    {
        private readonly TestBuildManager _buildManager;
        private readonly TestDesignTimeInputsBuildManagerBridge _bridge;
        private string? _lastCompiledFile;
        private string? _lastOutputPath;
        private ImmutableHashSet<string>? _lastSharedInputs;

        [Fact]
        public async Task ChangedFile_FiresTempPEDirty()
        {
            await _bridge.ApplyAsync(new DesignTimeInputsDelta(
                ImmutableHashSet.CreateRange(new string[] { "Resources1.Designer.cs" }),
                ImmutableHashSet<string>.Empty,
                new DesignTimeInputFileChange[] { new DesignTimeInputFileChange("Resources1.Designer.cs", false) },
                "C:\\TempPE"));

            // One file should have been added
            Assert.Single(_buildManager.DirtyItems);
            Assert.Equal("Resources1.Designer.cs", _buildManager.DirtyItems.First());
            Assert.Empty(_buildManager.DeletedItems);
        }

        [Fact]
        public async Task RemovedFile_FiresTempPEDeleted()
        {
            await _bridge.ApplyAsync(new DesignTimeInputsDelta(
                ImmutableHashSet.CreateRange(new string[] { "Resources1.Designer.cs" }),
                ImmutableHashSet<string>.Empty,
                Array.Empty<DesignTimeInputFileChange>(),
                ""));

            await _bridge.ApplyAsync(new DesignTimeInputsDelta(
               ImmutableHashSet<string>.Empty,
               ImmutableHashSet<string>.Empty,
               Array.Empty<DesignTimeInputFileChange>(),
               ""));

            // One file should have been added
            Assert.Empty(_buildManager.DirtyItems);
            Assert.Single(_buildManager.DeletedItems);
            Assert.Equal("Resources1.Designer.cs", _buildManager.DeletedItems.First());
        }

        [Fact]
        public async Task GetDesignTimeInputXmlAsync_HasCorrectArguments()
        {
            await _bridge.ApplyAsync(new DesignTimeInputsDelta(
                ImmutableHashSet.CreateRange(new string[] { "Resources1.Designer.cs" }),
                ImmutableHashSet<string>.Empty,
                new DesignTimeInputFileChange[] { new DesignTimeInputFileChange("Resources1.Designer.cs", false) },
                "C:\\TempPE"));

            await _bridge.GetDesignTimeInputXmlAsync("Resources1.Designer.cs");

            Assert.Equal("Resources1.Designer.cs", _lastCompiledFile);
            Assert.Equal("C:\\TempPE", _lastOutputPath);
            Assert.Equal(ImmutableHashSet<string>.Empty, _lastSharedInputs);
        }

        public DesignTimeInputsBuildManagerBridgeTests()
        {
            var threadingService = IProjectThreadingServiceFactory.Create();

            var changeTracker = Mock.Of<IDesignTimeInputsChangeTracker>();

            var compilerMock = new Mock<IDesignTimeInputsCompiler>();
            compilerMock.Setup(c => c.GetDesignTimeInputXmlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ImmutableHashSet<string>>()))
                .Callback<string, string, ImmutableHashSet<string>>((file, outputPath, sharedInputs) =>
                {
                    _lastCompiledFile = file;
                    _lastOutputPath = outputPath;
                    _lastSharedInputs = sharedInputs;
                });

            var project = UnconfiguredProjectFactory.Create(filePath: @"C:\MyProject\MyProject.csproj");

            _buildManager = new TestBuildManager();

            _bridge = new TestDesignTimeInputsBuildManagerBridge(project, threadingService, changeTracker, compilerMock.Object, _buildManager);
            _bridge.SkipInitialization = true;
        }

        public void Dispose()
        {
            ((IDisposable)_bridge).Dispose();
        }

        internal class TestDesignTimeInputsBuildManagerBridge : DesignTimeInputsBuildManagerBridge
        {
            public TestDesignTimeInputsBuildManagerBridge(UnconfiguredProject project, IProjectThreadingService threadingService, IDesignTimeInputsChangeTracker designTimeInputsChangeTracker, IDesignTimeInputsCompiler designTimeInputsCompiler, VSBuildManager buildManager)
                : base(project, threadingService, designTimeInputsChangeTracker, designTimeInputsCompiler, buildManager)
            {
            }

            public Task ApplyAsync(DesignTimeInputsDelta delta)
            {
                var input = IProjectVersionedValueFactory.Create(delta);

                return base.ApplyAsync(input);
            }
        }

        internal class TestBuildManager : VSBuildManager
        {
            public List<string> DeletedItems { get; } = new List<string>();
            public List<string> DirtyItems { get; } = new List<string>();

            internal TestBuildManager()
                : base(IProjectThreadingServiceFactory.Create(),
                       IUnconfiguredProjectCommonServicesFactory.Create(UnconfiguredProjectFactory.Create()))
            {
            }

            internal override void OnDesignTimeOutputDeleted(string outputMoniker)
            {
                DeletedItems.Add(outputMoniker);
            }

            internal override void OnDesignTimeOutputDirty(string outputMoniker)
            {
                DirtyItems.Add(outputMoniker);
            }

        }
    }
}
