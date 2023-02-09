﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using NuGet.SolutionRestoreManager;
using Microsoft.VisualStudio.ProjectSystem.PackageRestore;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore;

/// <summary>
///     Wraps a collection of <see cref="TargetFrameworkInfo"/> instances to implement the <see cref="IVsTargetFrameworks2"/>
///     interface for NuGet.
/// </summary>
internal class TargetFrameworksWrapper : ImmutablePropertyCollection<IVsTargetFrameworkInfo2, TargetFrameworkInfo>,  IVsTargetFrameworks2
{
    public TargetFrameworksWrapper(ImmutableList<TargetFrameworkInfo> targetFrameworks)
        : base(targetFrameworks, item => item.TargetFrameworkMoniker, item => new TargetFrameworkInfoWrapper(item))
    {
    }
}
