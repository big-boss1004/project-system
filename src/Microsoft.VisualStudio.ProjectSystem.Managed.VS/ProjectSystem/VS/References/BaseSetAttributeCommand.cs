﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    internal abstract class BaseSetAttributeCommand : IProjectSystemUpdateReferenceOperation
    {
        private readonly ConfiguredProject _selectedConfiguredProject;
        private readonly string _itemSpecification;
        private readonly AbstractReferenceHandler _referenceHandler;
        protected string _setValue = PropertySerializer.SimpleTypes.ToString(true);
        protected string _unsetValue = PropertySerializer.SimpleTypes.ToString(false);

        public BaseSetAttributeCommand(AbstractReferenceHandler abstractReferenceHandler, ConfiguredProject selectedConfiguredProject, string itemSpecification)
        {
            _referenceHandler = abstractReferenceHandler;
            _selectedConfiguredProject = selectedConfiguredProject;
            _itemSpecification = itemSpecification;
        }

        public async Task<bool> ApplyAsync(CancellationToken cancellationToken)
        {
            IProjectItem item = await GetProjectItemAsync();

            if (item == null)
            {
                return false;
            }

            await item.Metadata.SetPropertyValueAsync(ProjectReference.TreatAsUsedProperty, _setValue, null);

            return true;
        }

        public async Task<bool> RevertAsync(CancellationToken cancellationToken)
        {
            IProjectItem item = await GetProjectItemAsync();

            if (item == null)
            {
                return false;
            }

            await item.Metadata.SetPropertyValueAsync(ProjectReference.TreatAsUsedProperty, _unsetValue, null);

            return true;
        }

        private async Task<IProjectItem> GetProjectItemAsync()
        {
            var projectItems = await _referenceHandler.GetUnresolvedReferencesAsync(_selectedConfiguredProject);

            var item = projectItems
                .FirstOrDefault(c => c.EvaluatedInclude == _itemSpecification);
            return item;
        }
    }
}
