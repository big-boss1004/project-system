﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    internal class ReferenceItem : IVsReferenceItem
    {
        public ReferenceItem(string name, IVsReferenceProperties properties)
        {
            Name = name;
            Properties = properties;
        }

        public string Name { get; }

        public IVsReferenceProperties Properties { get; }
    }
}
