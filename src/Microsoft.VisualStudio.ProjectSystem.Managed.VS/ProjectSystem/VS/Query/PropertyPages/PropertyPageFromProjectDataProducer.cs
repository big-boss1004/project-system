﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles retrieving a set of <see cref="IPropertyPage"/>s from an <see cref="IProject"/>.
    /// </summary>
    internal class PropertyPageFromProjectDataProducer : QueryDataFromProviderStateProducerBase<UnconfiguredProject>
    {
        private readonly IPropertyPagePropertiesAvailableStatus _properties;
        private readonly IProjectStateProvider _projectStateProvider;

        public PropertyPageFromProjectDataProducer(IPropertyPagePropertiesAvailableStatus properties, IProjectStateProvider projectStateProvider)
        {
            _properties = properties;
            _projectStateProvider = projectStateProvider;
        }

        protected override Task<IEnumerable<IEntityValue>> CreateValuesAsync(IQueryExecutionContext queryExecutionContext, IEntityValue parent, UnconfiguredProject providerState)
        {
            queryExecutionContext.ReportProjectVersion(providerState);

            return PropertyPageDataProducer.CreatePropertyPageValuesAsync(queryExecutionContext, parent, providerState, _projectStateProvider, _properties);
        }
    }
}
