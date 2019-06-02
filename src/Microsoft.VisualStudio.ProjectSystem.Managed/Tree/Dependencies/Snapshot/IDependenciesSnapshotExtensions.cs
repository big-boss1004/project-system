﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

#nullable enable

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    internal static class IDependenciesSnapshotExtensions
    {
        public static IEnumerable<IDependency> GetFlatTopLevelDependencies(this IDependenciesSnapshot self)
        {
            foreach ((ITargetFramework _, ITargetedDependenciesSnapshot targetedSnapshot) in self.Targets)
            {
                foreach (IDependency dependency in targetedSnapshot.TopLevelDependencies)
                {
                    yield return dependency;
                }
            }
        }
    }
}
