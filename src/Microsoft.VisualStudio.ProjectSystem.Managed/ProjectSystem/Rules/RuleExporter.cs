﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.Rules
{
    /// <summary>
    ///     Responsible for exporting our embedded rules so that CPS can pick them.
    /// </summary>
    internal static class RuleExporter
    {
        /// <summary>
        ///     Represents the evaluation properties that is used for AppDesigner folder services.
        /// </summary>
        [ExportRule(nameof(AppDesigner), PropertyPageContexts.ProjectSubscriptionService)]
        [AppliesTo(ProjectCapability.AppDesigner)]
        public static int AppDesignerRule;

        /// <summary>
        ///     Represents the design-time build items containing the compiler command-line that is passed to Roslyn.
        /// </summary>
        [ExportRule(nameof(CompilerCommandLineArgs), PropertyPageContexts.ProjectSubscriptionService)]
        [AppliesTo(ProjectCapability.DotNetLanguageService)]
        public static int CompilerCommandLineArgsRule;

        /// <summary>
        ///     Represents the evaluation properties that are passed to Roslyn.
        /// </summary>
        [ExportRule(nameof(LanguageService), PropertyPageContexts.ProjectSubscriptionService)]
        [AppliesTo(ProjectCapability.DotNetLanguageService)]
        public static int LanguageServiceRule;

        /// <summary>
        ///     Represents the design-time build items containing resolved references path.
        /// </summary>
        [ExportRule(nameof(ResolvedCompilationReference), PropertyPageContexts.ProjectSubscriptionService)]
        [AppliesTo(ProjectCapability.DotNet + "+ !" + ProjectCapabilities.SharedAssetsProject)]
        public static int ResolvedCompilationReferencedRule;        

        /// <summary>
        ///     Represents the evaluation properties that are passed to NuGet.
        /// </summary>
        [ExportRule(nameof(NuGetRestore), PropertyPageContexts.ProjectSubscriptionService)]
        [AppliesTo(ProjectCapability.PackageReferences)]
        public static int NuGetRestoreRule;

        /// <summary>
        ///     Represents evaluation items containing marker files indicating that reference projects have out of date references.
        /// </summary>
        [ExportRule(nameof(CopyUpToDateMarker), PropertyPageContexts.ProjectSubscriptionService)]
        [AppliesTo(ProjectCapability.DotNet + "+ !" + ProjectCapabilities.SharedAssetsProject)]
        public static int CopyUpToDateMarkerRule;

        /// <summary>
        ///     Represents design-time build items containing the input files into the build.
        /// </summary>
        [ExportRule(nameof(UpToDateCheckInput), PropertyPageContexts.ProjectSubscriptionService)]
        [AppliesTo(ProjectCapability.DotNet + "+ !" + ProjectCapabilities.SharedAssetsProject)]
        public static int UpToDateCheckInputRule;

        /// <summary>
        ///     Represents design-time build items containing the output files of the build.
        /// </summary>
        [ExportRule(nameof(UpToDateCheckOutput), PropertyPageContexts.ProjectSubscriptionService)]
        [AppliesTo(ProjectCapability.DotNet + "+ !" + ProjectCapabilities.SharedAssetsProject)]
        public static int UpToDateCheckOutputRule;

        /// <summary>
        ///     Represents design-time build items containing a mapping between input and the output files of the build.
        /// </summary>
        [ExportRule(nameof(UpToDateCheckBuilt), PropertyPageContexts.ProjectSubscriptionService)]
        [AppliesTo(ProjectCapability.DotNet + "+ !" + ProjectCapabilities.SharedAssetsProject)]
        public static int UpToDateCheckBuiltRule;

        /// <summary>
        ///     Represents the evaluation properties representings source control bindings, 
        ///     typically used in projects connected to Team Foundation Source Control.
        /// </summary>
        [ExportRule(nameof(SourceControl), PropertyPageContexts.Invisible)]
        [AppliesTo(ProjectCapability.DotNet)]
        public static int SourceControlRule;
    }
}
