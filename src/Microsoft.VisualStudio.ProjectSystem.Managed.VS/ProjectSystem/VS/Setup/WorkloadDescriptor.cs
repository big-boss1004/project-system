﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Setup
{
    /// <summary>
    ///     Represents a mapping from a .NET workload to one or more Visual Studio components.
    /// </summary>
    /// <remarks>
    ///     Note that this represents a .NET workload, not a VS workload.
    /// </remarks>
    internal readonly struct WorkloadDescriptor
    {
        public WorkloadDescriptor(string workloadName, string visualStudioComponentIds)
        {
            WorkloadName = workloadName;
            string[] vsComponentIds = visualStudioComponentIds.Split(Delimiter.Semicolon, StringSplitOptions.RemoveEmptyEntries);
            VisualStudioComponentIds = new HashSet<string>(vsComponentIds, StringComparers.VisualStudioSetupComponentIds);
        }

        /// <summary>
        ///     Gets the name of the .NET workload.
        /// </summary>
        public string WorkloadName { get; }

        /// <summary>
        ///     Gets the Visual Studio setup component ID corresponding to the .NET workload.
        /// </summary>
        public ISet<string> VisualStudioComponentIds { get; }

        public bool Equals(WorkloadDescriptor other)
        {
            return StringComparers.WorkloadNames.Equals(WorkloadName, other.WorkloadName)
                && VisualStudioComponentIds.SetEquals(other.VisualStudioComponentIds);
        }

        public override bool Equals(object? obj)
        {
            return obj is WorkloadDescriptor workloadDescriptor && Equals(workloadDescriptor);
        }

        public override int GetHashCode()
        {
            int hashCode = StringComparers.WorkloadNames.GetHashCode(WorkloadName) * -1521134295;

            foreach (string componentId in VisualStudioComponentIds)
            {
                hashCode |= StringComparers.VisualStudioSetupComponentIds.GetHashCode(componentId);
            }

            return hashCode;
        }
    }
}
