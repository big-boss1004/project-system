﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal enum PackageDiagnosticMessageSeverity
    {
        Info,
        Warning,
        Error,
    }

    internal class PackageDiagnosticDependencyModel : DependencyModel
    {
        private static readonly ProjectTreeFlags s_errorFlags = new DependencyFlagCache(
            add: DependencyTreeFlags.PackageDiagnostic +
                 DependencyTreeFlags.PackageErrorDiagnostic).Get(isResolved: false, isImplicit: false);

        private static readonly ProjectTreeFlags s_warningFlags = new DependencyFlagCache(
            add: DependencyTreeFlags.PackageDiagnostic +
                 DependencyTreeFlags.PackageWarningDiagnostic).Get(isResolved: false, isImplicit: false);

        private static readonly DependencyIconSet s_errorIconSet = new DependencyIconSet(
            icon: ManagedImageMonikers.ErrorSmall,
            expandedIcon: ManagedImageMonikers.ErrorSmall,
            unresolvedIcon: ManagedImageMonikers.ErrorSmall,
            unresolvedExpandedIcon: ManagedImageMonikers.ErrorSmall);

        private static readonly DependencyIconSet s_warningIconSet = new DependencyIconSet(
            icon: ManagedImageMonikers.WarningSmall,
            expandedIcon: ManagedImageMonikers.WarningSmall,
            unresolvedIcon: ManagedImageMonikers.WarningSmall,
            unresolvedExpandedIcon: ManagedImageMonikers.WarningSmall);

        private readonly PackageDiagnosticMessageSeverity _severity;

        public override DependencyIconSet IconSet => _severity == PackageDiagnosticMessageSeverity.Error
            ? s_errorIconSet
            : s_warningIconSet;

        public override string Name { get; }

        public override int Priority => _severity == PackageDiagnosticMessageSeverity.Error
            ? GraphNodePriority.DiagnosticsError
            : GraphNodePriority.DiagnosticsWarning;

        public override string ProviderType => PackageRuleHandler.ProviderTypeString;

        public PackageDiagnosticDependencyModel(
            string originalItemSpec,
            PackageDiagnosticMessageSeverity severity,
            string code,
            string message,
            bool isVisible,
            IImmutableDictionary<string, string> properties)
            : base(
                originalItemSpec,
                originalItemSpec,
                flags: severity == PackageDiagnosticMessageSeverity.Error
                    ? s_errorFlags
                    : s_warningFlags,
                isResolved: false,
                isImplicit: false,
                properties: properties,
                isTopLevel: false,
                isVisible: isVisible)
        {
            Requires.NotNullOrEmpty(originalItemSpec, nameof(originalItemSpec));
            Requires.NotNullOrEmpty(message, nameof(message));

            _severity = severity;

            Name = message;
            Caption = string.IsNullOrWhiteSpace(code) ? message : string.Concat(code.ToUpperInvariant(), " ", message);
        }
    }
}
