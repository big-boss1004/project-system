﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload
{
    public interface IProjectHotReloadSessionCallback
    {
        bool SupportsRestart { get; }

        Task OnAfterChangesAppliedAsync(CancellationToken cancellationToken);

        Task<bool> StopProjectAsync(CancellationToken cancellationToken);

        Task<bool> RestartProjectAsync(CancellationToken cancellationToken);

        // TODO: IDeltaApplier will be defined elsewhere. Add this back in once we
        // can reference it in the final location.
        object GetDeltaApplier();
    }
}
