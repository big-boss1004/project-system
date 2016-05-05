﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Input;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    // Opens the Project Designer ("Property Pages") when selecting the Open menu item on the AppDesigner folder
    [ProjectCommand(CommandGroup.VisualStudioStandard97, VisualStudioStandard97CommandId.Open)]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    [OrderPrecedence(1000)] 
    internal class OpenProjectDesignerCommand : AbstractOpenProjectDesignerCommand
    {
        [ImportingConstructor]
        public OpenProjectDesignerCommand(IProjectDesignerService designerService)
            : base(designerService)
        {
        }
    }
}
