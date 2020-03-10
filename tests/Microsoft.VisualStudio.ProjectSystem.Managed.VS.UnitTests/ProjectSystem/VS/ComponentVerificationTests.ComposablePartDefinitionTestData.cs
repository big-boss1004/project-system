﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Composition;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    public partial class ComponentVerificationTests
    {
        internal class ComposablePartDefinitionTestData : TheoryData<Type>
        {
            public ComposablePartDefinitionTestData()
            {
                var catalog = ComponentComposition.Instance.Catalog;

                foreach (ComposablePartDefinition part in catalog.Parts)
                {
                    Add(part.Type);
                }
            }
        }
    }
}
