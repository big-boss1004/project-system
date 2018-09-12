﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.Debug;

using Moq;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    public class DebugFrameworkMenuTextUpdaterTests
    {
        [Fact]
        public void Exec_DoesNothing()
        {
            var command = new DebugFrameworkPropertyMenuTextUpdater(null);
            DebugFrameworkPropertyMenuTextUpdater.ExecHandler(command, EventArgs.Empty);
            Assert.True(command.Visible);
            Assert.Equal("", command.Text);
            Assert.False(command.Checked);
            Assert.True(command.Enabled);
        }

        [Fact]
        public void QueryStatus_NoActiveProject()
        {
            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromSingleDotNetStartupProject<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns((IActiveDebugFrameworkServices)null);

            var command = new TestDebugFrameworkPropertyMenuTextUpdater(startupHelper.Object);
            command.QueryStatus();
            Assert.True(command.Visible);
            Assert.Equal("", command.Text);
            Assert.False(command.Checked);
            Assert.True(command.Enabled);
        }

        [Fact]
        public void QueryStatus_NullFrameworks()
        {
            var activeDebugFrameworkSvcs = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync(null)
                                               .ImplementGetProjectFrameworksAsync(null);
            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromSingleDotNetStartupProject<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(activeDebugFrameworkSvcs.Object);

            var command = new TestDebugFrameworkPropertyMenuTextUpdater(startupHelper.Object);
            command.QueryStatus();
            Assert.True(command.Visible);
            Assert.Equal("", command.Text);
            Assert.False(command.Checked);
            Assert.True(command.Enabled);
        }

        [Fact]
        public void QueryStatus_FrameworksLessThan2()
        {
            var activeDebugFrameworkSvcs = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync(null)
                                               .ImplementGetProjectFrameworksAsync(new List<string>() { "net45" });
            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromSingleDotNetStartupProject<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(activeDebugFrameworkSvcs.Object);

            var command = new TestDebugFrameworkPropertyMenuTextUpdater(startupHelper.Object);
            command.QueryStatus();
            Assert.True(command.Visible);
            Assert.Equal("", command.Text);
            Assert.False(command.Checked);
            Assert.True(command.Enabled);
        }

        [Fact]
        public void QueryStatus_FrameworkNoAciive()
        {
            var activeDebugFrameworkSvcs = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync(null)
                                               .ImplementGetProjectFrameworksAsync(new List<string>() { "net461", "netcoreapp1.0" });
            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromSingleDotNetStartupProject<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(activeDebugFrameworkSvcs.Object);

            var command = new TestDebugFrameworkPropertyMenuTextUpdater(startupHelper.Object);
            command.QueryStatus();
            Assert.True(command.Visible);
            Assert.Equal(string.Format(VSResources.DebugFrameworkMenuText, "net461"), command.Text);
            Assert.False(command.Checked);
            Assert.True(command.Enabled);
        }
        [Fact]
        public void QueryStatus_FrameworkNoMatchingAciive()
        {
            var activeDebugFrameworkSvcs = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync("net45")
                                               .ImplementGetProjectFrameworksAsync(new List<string>() { "net461", "netcoreapp1.0" });
            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromSingleDotNetStartupProject<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(activeDebugFrameworkSvcs.Object);

            var command = new TestDebugFrameworkPropertyMenuTextUpdater(startupHelper.Object);
            command.QueryStatus();
            Assert.True(command.Visible);
            Assert.Equal(string.Format(VSResources.DebugFrameworkMenuText, "net461"), command.Text);
            Assert.False(command.Checked);
            Assert.True(command.Enabled);
        }

        [Fact]
        public void QueryStatus_FrameworkValidAciive()
        {
            var activeDebugFrameworkSvcs = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync("netcoreapp1.0")
                                               .ImplementGetProjectFrameworksAsync(new List<string>() { "net461", "netcoreapp1.0" });
            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromSingleDotNetStartupProject<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(activeDebugFrameworkSvcs.Object);

            var command = new TestDebugFrameworkPropertyMenuTextUpdater(startupHelper.Object);
            command.QueryStatus();
            Assert.True(command.Visible);
            Assert.Equal(string.Format(VSResources.DebugFrameworkMenuText, "netcoreapp1.0"), command.Text);
            Assert.False(command.Checked);
            Assert.True(command.Enabled);
        }

        private class TestDebugFrameworkPropertyMenuTextUpdater : DebugFrameworkPropertyMenuTextUpdater
        {
            public TestDebugFrameworkPropertyMenuTextUpdater(IStartupProjectHelper startupHelper)
                : base(startupHelper)
            {
            }

            protected override void ExecuteSynchronously(Func<Task> asyncFunction)
            {
                asyncFunction.Invoke().Wait();
            }
        }
    }
}
