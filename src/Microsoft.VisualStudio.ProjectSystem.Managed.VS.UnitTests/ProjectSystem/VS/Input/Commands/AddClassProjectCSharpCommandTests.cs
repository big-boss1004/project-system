﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    public class AddClassProjectCSharpCommandTests : AbstractAddClassProjectCommandTests
    {
        internal override string DirName { get; } = "Visual C# Items";

        internal override AbstractAddClassProjectCommand CreateInstance(IPhysicalProjectTree tree, IUnconfiguredProjectVsServices services, IVsUIService<SVsAddProjectItemDlg, IVsAddProjectItemDlg> addItemDialog)
        {
            return new AddClassProjectCSharpCommand(tree, services, addItemDialog);
        }
    }
}
