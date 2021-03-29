﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles retrieving an <see cref="IUIPropertyEditor"/> base on an ID.
    /// </summary>
    internal class UIPropertyEditorByIdDataProducer : QueryDataByIdProducerBase
    {
        private readonly IUIPropertyEditorPropertiesAvailableStatus _properties;
        private readonly IProjectService2 _projectService;

        public UIPropertyEditorByIdDataProducer(IUIPropertyEditorPropertiesAvailableStatus properties, IProjectService2 projectService)
        {
            Requires.NotNull(properties, nameof(properties));
            Requires.NotNull(projectService, nameof(projectService));
            _properties = properties;
            _projectService = projectService;
        }

        protected override Task<IEntityValue?> TryCreateEntityOrNullAsync(IQueryExecutionContext executionContext, EntityIdentity id)
        {
            if (id.KeysCount == 4
                && id.TryGetValue(ProjectModelIdentityKeys.ProjectPath, out string projectPath)
                && id.TryGetValue(ProjectModelIdentityKeys.PropertyPageName, out string propertyPageName)
                && id.TryGetValue(ProjectModelIdentityKeys.UIPropertyName, out string propertyName)
                && id.TryGetValue(ProjectModelIdentityKeys.EditorName, out string editorName))
            {
                return UIPropertyEditorDataProducer.CreateEditorValueAsync(
                    executionContext,
                    id,
                    _projectService,
                    projectPath,
                    propertyPageName,
                    propertyName,
                    editorName,
                    _properties);
            }

            return NullEntityValue;
        }
    }
}
