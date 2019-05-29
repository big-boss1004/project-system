﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    internal enum RuleHandlerType
    {
        /// <summary>
        ///     The <see cref="IDependenciesRuleHandler"/> handles changes 
        ///     to evaluation rules.
        /// </summary>
        Evaluation,

        /// <summary>
        ///     The <see cref="IDependenciesRuleHandler"/> handles changes 
        ///     to design-time build rules.
        /// </summary>
        DesignTimeBuild,
    }
}
