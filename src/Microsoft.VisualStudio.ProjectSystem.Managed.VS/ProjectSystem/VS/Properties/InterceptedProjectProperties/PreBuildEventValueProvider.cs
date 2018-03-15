﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.InterceptedProjectProperties
{
    [ExportInterceptingPropertyValueProvider(PreBuildEvent, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal class PreBuildEventValueProvider : AbstractBuildEventValueProvider
    {
        private const string PreBuildEvent = "PreBuildEvent";
        private const string TargetName = "PreBuild";

        [ImportingConstructor]
        public PreBuildEventValueProvider(
            IProjectLockService projectLockService,
            UnconfiguredProject project)
            : base(projectLockService,
                   project,
                   new PreBuildEventHelper())
        { }

        internal class PreBuildEventHelper : AbstractBuildEventHelper
        {
            internal PreBuildEventHelper()
                : base(PreBuildEvent,
                       TargetName,
                       target => target.BeforeTargets,
                       target => { target.BeforeTargets = PreBuildEvent; })
            { }
        }
    }
}
