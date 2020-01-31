﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Net.NetworkInformation;
using EnvDTE;
using Microsoft.VisualStudio.Mocks;
using Microsoft.VisualStudio.Threading;
using Xunit;

namespace Microsoft.VisualStudio.SolutionExplorer
{
    public class ProjectNodeExtenderTests
    {
        [Fact]
        public void WhenNodeIsNull_NoCommandHandlerIsReturned()
        {
            var extender = new ProjectNodeExtender(GetJoinableTaskContext(), IServiceProviderFactory.ImplementGetService(t => null));

            var commandHandler = extender.ProvideCommandHandler(null!);

            Assert.Null(commandHandler);
        }

        [Fact]
        public void WhenSelectionKindIsWrong_NoCommandHandlerIsReturned()
        {
            var extender = new ProjectNodeExtender(GetJoinableTaskContext(), IServiceProviderFactory.ImplementGetService(t => null));
            var node = WorkspaceVisualNodeBaseFactory.Implement(
                selectionKind: Guid.Parse("{95D7E5E9-08FA-40FB-9010-2CCEEC6D54C1}"),
                nodeMoniker: "Test.csproj",
                selectionMoniker: "Test.csproj");

            var commandHandler = extender.ProvideCommandHandler(node);

            Assert.Null(commandHandler);
        }

        [Fact]
        public void WhenExtensionIsWrong_NoCommandHandlerIsReturned()
        {
            var extender = new ProjectNodeExtender(GetJoinableTaskContext(), IServiceProviderFactory.ImplementGetService(t => null));
            var node = WorkspaceVisualNodeBaseFactory.Implement(
                selectionKind: CloudEnvironment.SolutionViewProjectGuid,
                nodeMoniker: "Test.notMyProj",
                selectionMoniker: "Test.notMyProj");

            var commandHandler = extender.ProvideCommandHandler(node);

            Assert.Null(commandHandler);
        }

        [Fact]
        public void WhenNodeRepresentsAManagedProject_ACommandHandlerIsReturned()
        {
            var extender = new ProjectNodeExtender(GetJoinableTaskContext(), IServiceProviderFactory.ImplementGetService(t => null));
            var node = WorkspaceVisualNodeBaseFactory.Implement(
                selectionKind: CloudEnvironment.SolutionViewProjectGuid,
                nodeMoniker: "Test.csproj",
                selectionMoniker: "Test.csproj");

            var commandHandler = extender.ProvideCommandHandler(node);

            Assert.NotNull(commandHandler);
        }

        private JoinableTaskContext GetJoinableTaskContext()
        {
#pragma warning disable VSSDK005 // Avoid instantiating JoinableTaskContext
            return new JoinableTaskContext();
#pragma warning restore VSSDK005 // Avoid instantiating JoinableTaskContext
        }
    }
}
