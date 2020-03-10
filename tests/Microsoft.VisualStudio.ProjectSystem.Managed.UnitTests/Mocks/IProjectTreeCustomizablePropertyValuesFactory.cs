﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE file in the project root for more information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectTreeCustomizablePropertyValuesFactory
    {
        public static IProjectTreeCustomizablePropertyValues Create()
        {
            return Mock.Of<IProjectTreeCustomizablePropertyValues>();
        }
    }
}
