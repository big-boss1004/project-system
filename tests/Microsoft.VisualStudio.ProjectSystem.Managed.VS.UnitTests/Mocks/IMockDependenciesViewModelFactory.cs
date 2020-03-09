﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IMockDependenciesViewModelFactory
    {
        public static IDependenciesViewModelFactory Implement(
            ImageMoniker? getDependenciesRootIcon = null,
            IEnumerable<IDependencyModel>? createRootViewModel = null,
            IEnumerable<IDependencyModel>? createTargetViewModel = null,
            MockBehavior mockBehavior = MockBehavior.Strict)
        {
            var mock = new Mock<IDependenciesViewModelFactory>(mockBehavior);

            if (getDependenciesRootIcon.HasValue)
            {
                mock.Setup(x => x.GetDependenciesRootIcon(It.IsAny<bool>())).Returns(getDependenciesRootIcon.Value);
            }

            if (createRootViewModel != null)
            {
                foreach (var d in createRootViewModel)
                {
                    mock.Setup(x => x.CreateGroupNodeViewModel(
                            It.Is<string>(t => string.Equals(t, d.ProviderType, StringComparison.OrdinalIgnoreCase)),
                            false))
                        .Returns(d.ToViewModel(false));
                    mock.Setup(x => x.CreateGroupNodeViewModel(
                            It.Is<string>(t => string.Equals(t, d.ProviderType, StringComparison.OrdinalIgnoreCase)),
                            true))
                        .Returns(d.ToViewModel(true));
                }
            }

            if (createTargetViewModel != null)
            {
                foreach (var d in createTargetViewModel)
                {
                    mock.Setup(x => x.CreateTargetViewModel(
                            It.Is<ITargetFramework>(t => string.Equals(t.FullName, d.Caption, StringComparison.OrdinalIgnoreCase)),
                            false))
                        .Returns(d.ToViewModel(false));
                }
            }

            return mock.Object;
        }
    }
}
