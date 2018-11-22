﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.LanguageServices;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    internal interface ICrossTargetRuleHandler<T> where T : IRuleChangeContext
    {
        /// <summary>
        ///     Gets the rule this handler handles.
        /// </summary>
        /// <value>
        ///     A <see cref="string"/> containing the rule that this <see cref="ICrossTargetRuleHandler{T}"/> 
        ///     handles.
        /// </value>
        ImmutableHashSet<string> GetRuleNames(RuleHandlerType handlerType);

        /// <summary>
        ///     Gets a value indicating the handler should be invoked even for updates with no underlying project changes (e.g. broken design time builds).
        /// </summary>
        bool ReceiveUpdatesWithEmptyProjectChange { get; }

        /// <summary>
        ///     Handles the specified set of changes to a rule, and applies them
        ///     to the given <see cref="ITargetedProjectContext"/>.
        /// </summary>
        void Handle(
            IImmutableDictionary<string, IProjectChangeDescription> changesByRuleName,
            ITargetFramework targetFramework,
            T ruleChangeContext);
    }
}
