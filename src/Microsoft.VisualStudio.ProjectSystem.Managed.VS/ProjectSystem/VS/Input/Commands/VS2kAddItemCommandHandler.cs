﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.Shell.Interop;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ExportCommandGroup(CommandGroup.VisualStudioStandard2k)]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class VS2kAddItemCommandHandler : AbstractAddItemCommandHandler
    {
        private static readonly ImmutableDictionary<long, ImmutableArray<TemplateDetails>> s_templateDetails = ImmutableDictionary<long, ImmutableArray<TemplateDetails>>.Empty
            //                     Command Id                                        Capabilities                                                        DirNamePackageGuid          DirNameResourceId                                       TemplateNameResourceId
            .CreateTemplateDetails(VisualStudioStandard2KCommandId.AddForm,          ProjectCapability.CSharp,       ProjectCapability.WindowsForms,     LegacyCSharpPackageGuid,    LegacyCSharpStringResourceIds.IDS_PROJECTITEMTYPE_STR,  LegacyCSharpStringResourceIds.IDS_TEMPLATE_NEWWFCWIN32FORM  )
            .CreateTemplateDetails(VisualStudioStandard2KCommandId.AddUserControl,   ProjectCapability.CSharp,       ProjectCapability.WindowsForms,     LegacyCSharpPackageGuid,    LegacyCSharpStringResourceIds.IDS_PROJECTITEMTYPE_STR,  LegacyCSharpStringResourceIds.IDS_TEMPLATE_NEWUSERCONTROL   )
            .CreateTemplateDetails(VisualStudioStandard2KCommandId.AddComponent,     ProjectCapability.CSharp,       ProjectCapability.WindowsForms,     LegacyCSharpPackageGuid,    LegacyCSharpStringResourceIds.IDS_PROJECTITEMTYPE_STR,  LegacyCSharpStringResourceIds.IDS_TEMPLATE_NEWWFCCOMPONENT  )

            .CreateTemplateDetails(VisualStudioStandard2KCommandId.AddForm,          ProjectCapability.VisualBasic,  ProjectCapability.WindowsForms,     LegacyVBPackageGuid,        LegacyVBStringResourceIds.IDS_VSDIR_VBPROJECTFILES,     LegacyVBStringResourceIds.IDS_VSDIR_ITEM_WINFORM            )
            .CreateTemplateDetails(VisualStudioStandard2KCommandId.AddUserControl,   ProjectCapability.VisualBasic,  ProjectCapability.WindowsForms,     LegacyVBPackageGuid,        LegacyVBStringResourceIds.IDS_VSDIR_VBPROJECTFILES,     LegacyVBStringResourceIds.IDS_VSDIR_ITEM_USERCTRL           )
            .CreateTemplateDetails(VisualStudioStandard2KCommandId.AddComponent,     ProjectCapability.VisualBasic,  ProjectCapability.WindowsForms,     LegacyVBPackageGuid,        LegacyVBStringResourceIds.IDS_VSDIR_VBPROJECTFILES,     LegacyVBStringResourceIds.IDS_VSDIR_ITEM_COMPONENT          )

            .CreateTemplateDetails(VisualStudioStandard2KCommandId.AddModule,        ProjectCapability.VisualBasic,                                      LegacyVBPackageGuid,        LegacyVBStringResourceIds.IDS_VSDIR_VBPROJECTFILES,     LegacyVBStringResourceIds.IDS_VSDIR_ITEM_MODULE             );

        protected override ImmutableDictionary<long, ImmutableArray<TemplateDetails>> GetTemplateDetails() => s_templateDetails;

        [ImportingConstructor]
        public VS2kAddItemCommandHandler(ConfiguredProject configuredProject, IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, IVsUIService<IVsAddProjectItemDlg> addItemDialog, IVsUIService<SVsShell, IVsShell> vsShell)
            : base(configuredProject, projectTree, projectVsServices, addItemDialog, vsShell)
        {
        }
    }
}
