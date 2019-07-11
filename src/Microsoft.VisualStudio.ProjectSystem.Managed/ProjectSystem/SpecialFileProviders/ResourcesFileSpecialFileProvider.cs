﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    [ExportSpecialFileProvider(SpecialFiles.AssemblyResource)]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class ResourcesFileSpecialFileProvider : AbstractSpecialFileProvider
    {
        [ImportingConstructor]
        public ResourcesFileSpecialFileProvider(IPhysicalProjectTree projectTree,
                                                [Import(ExportContractNames.ProjectItemProviders.SourceFiles)] IProjectItemProvider sourceItemsProvider,
                                                [Import(AllowDefault = true)] Lazy<ICreateFileFromTemplateService> templateFileCreationService,
                                                IFileSystem fileSystem,
                                                ISpecialFilesManager specialFilesManager)
            : base(projectTree, sourceItemsProvider, templateFileCreationService, fileSystem, specialFilesManager)
        {
        }

        protected override string Name => "Resources.resx";

        protected override string TemplateName => "ResourceInternal.zip";
    }
}
