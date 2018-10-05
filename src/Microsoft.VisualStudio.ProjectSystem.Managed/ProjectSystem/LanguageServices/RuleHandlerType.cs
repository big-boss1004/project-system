﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal enum RuleHandlerType
    {
        /// <summary>
        ///     The <see cref="IProjectEvaluationHandler"/> handles changes 
        ///     to evaluation rules.
        /// </summary>
        Evaluation,

        /// <summary>
        ///     The <see cref="IProjectEvaluationHandler"/> handles changes 
        ///     to design-time build rules.
        /// </summary>
        DesignTimeBuild,
    }
}
