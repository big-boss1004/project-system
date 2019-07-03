﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Moq;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectTreeServiceStateFactory
    {
        public static IProjectTreeServiceState Create()
        {
            return Mock.Of<IProjectTreeServiceState>();
        }

        public static IProjectTreeServiceState ImplementTree(Func<IProjectTree> treeAction)
        {
            return ImplementTree(treeAction, null);
        }

        public static IProjectTreeServiceState ImplementTree(Func<IProjectTree> treeAction, Func<IProjectTreeProvider> treeProviderAction)
        {
            var mock = new Mock<IProjectTreeServiceState>();
            mock.SetupGet(s => s.Tree)
                .Returns(treeAction);

            if (treeProviderAction != null)
                mock.SetupGet(s => s.TreeProvider)
                    .Returns(treeProviderAction);

            return mock.Object;
        }
    }
}
