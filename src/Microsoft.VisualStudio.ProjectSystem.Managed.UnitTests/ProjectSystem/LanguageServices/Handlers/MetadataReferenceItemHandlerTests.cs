﻿using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Logging;

using Moq;

using Xunit;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    public class MetadataReferenceItemHandlerTests : CommandLineHandlerTestBase
    {
        [Fact]
        public void Constructor_NullAsProject_ThrowsArgumentNull()
        {
            var context = IWorkspaceProjectContextMockFactory.Create();

            Assert.Throws<ArgumentNullException>("project", () =>
            {
                new MetadataReferenceItemHandler((UnconfiguredProject)null);
            });
        }

        [Fact]
        public void DuplicateMetadataReferencesPushedToWorkspace()
        {
            var referencesPushedToWorkspace = new HashSet<string>(StringComparers.Paths);
            void onReferenceAdded(string s) => referencesPushedToWorkspace.Add(s);
            void onReferenceRemoved(string s) => referencesPushedToWorkspace.Remove(s);

            var project = UnconfiguredProjectFactory.Create(filePath: @"C:\Myproject.csproj");
            var context = IWorkspaceProjectContextMockFactory.CreateForMetadataReferences(project, onReferenceAdded, onReferenceRemoved);
            var logger = Mock.Of<IProjectLogger>();

            var handler = CreateInstance(project, context);
            var projectDir = Path.GetDirectoryName(project.FullPath);
            var added = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new[] { @"/reference:C:\Assembly1.dll", @"/reference:C:\Assembly2.dll", @"/reference:C:\Assembly1.dll" }, baseDirectory: projectDir, sdkDirectory: null));
            var empty = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new string[] { }, baseDirectory: projectDir, sdkDirectory: null));

            handler.Handle(10, added: added, removed: empty, isActiveContext: true, logger: logger);

            AssertEx.CollectionLength(referencesPushedToWorkspace, 2);
            Assert.Contains(@"C:\Assembly1.dll", referencesPushedToWorkspace);
            Assert.Contains(@"C:\Assembly2.dll", referencesPushedToWorkspace);

            var removed = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new[] { @"/reference:C:\Assembly1.dll", @"/reference:C:\Assembly1.dll" }, baseDirectory: projectDir, sdkDirectory: null));
            handler.Handle(10, added: empty, removed: removed, isActiveContext: true, logger: logger);

            Assert.Single(referencesPushedToWorkspace);
            Assert.Contains(@"C:\Assembly2.dll", referencesPushedToWorkspace);
        }

        [Fact]
        public void RootedReferencesPushedToWorkspace()
        {
            var referencesPushedToWorkspace = new HashSet<string>(StringComparers.Paths);
            void onReferenceAdded(string s) => referencesPushedToWorkspace.Add(s);
            void onReferenceRemoved(string s) => referencesPushedToWorkspace.Remove(s);

            var project = UnconfiguredProjectFactory.Create(filePath: @"C:\ProjectFolder\Myproject.csproj");
            var context = IWorkspaceProjectContextMockFactory.CreateForMetadataReferences(project, onReferenceAdded, onReferenceRemoved);
            var logger = Mock.Of<IProjectLogger>();

            var handler = CreateInstance(project, context);
            var projectDir = Path.GetDirectoryName(project.FullPath);
            var added = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new[] { @"/reference:Assembly1.dll", @"/reference:C:\ProjectFolder\Assembly2.dll", @"/reference:..\ProjectFolder\Assembly3.dll" }, baseDirectory: projectDir, sdkDirectory: null));
            var removed = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new string[] { }, baseDirectory: projectDir, sdkDirectory: null));

            handler.Handle(10, added: added, removed: removed, isActiveContext: true, logger: logger);

            AssertEx.CollectionLength(referencesPushedToWorkspace, 3);
            Assert.Contains(@"C:\ProjectFolder\Assembly1.dll", referencesPushedToWorkspace);
            Assert.Contains(@"C:\ProjectFolder\Assembly2.dll", referencesPushedToWorkspace);
            Assert.Contains(@"C:\ProjectFolder\Assembly3.dll", referencesPushedToWorkspace);
        }
        
        internal override ICommandLineHandler CreateInstance()
        {
            return CreateInstance(null, null);
        }

        private MetadataReferenceItemHandler CreateInstance(UnconfiguredProject project = null, IWorkspaceProjectContext context = null)
        {
            project ??= UnconfiguredProjectFactory.Create();

            var handler = new MetadataReferenceItemHandler(project);
            if (context != null)
                handler.Initialize(context);

            return handler;
        }
    }
}
