﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    internal class PackageReferenceHandler : AbstractReferenceHandler
    {
        internal PackageReferenceHandler() :
            base(ProjectSystemReferenceType.Package)
        { }

        protected override Task RemoveReferenceAsync(ConfiguredProjectServices services,
            ProjectSystemReferenceInfo referencesInfo)
        {
            Assumes.Present(services.PackageReferences);

            return services.PackageReferences.RemoveAsync(referencesInfo.ItemSpecification);
        }

        protected override async Task<IEnumerable<IProjectItem>> GetUnresolvedReferencesAsync(ConfiguredProjectServices services)
        {
            Assumes.Present(services.PackageReferences);

            return (await services.PackageReferences.GetUnresolvedReferencesAsync()).Cast<IProjectItem>();
        }
    }
}
