﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles the creation of <see cref="IPropertyPage"/> instances and populating the requested members.
    /// </summary>
    internal static class UIPropertyDataProducer
    {
        public static IEntityValue CreateUIPropertyValue(IQueryExecutionContext queryExecutionContext, IEntityValue parent, IProjectState cache, QueryProjectPropertiesContext propertiesContext, BaseProperty property, int order, IUIPropertyPropertiesAvailableStatus requestedProperties)
        {
            Requires.NotNull(parent, nameof(parent));
            Requires.NotNull(property, nameof(property));

            var identity = new EntityIdentity(
                ((IEntityWithId)parent).Id,
                new KeyValuePair<string, string>[]
                {
                    new(ProjectModelIdentityKeys.UIPropertyName, property.Name)
                });

            return CreateUIPropertyValue(queryExecutionContext, identity, cache, propertiesContext, property, order, requestedProperties);
        }

        public static IEntityValue CreateUIPropertyValue(IQueryExecutionContext queryExecutionContext, EntityIdentity id, IProjectState cache, QueryProjectPropertiesContext propertiesContext, BaseProperty property, int order, IUIPropertyPropertiesAvailableStatus requestedProperties)
        {
            Requires.NotNull(property, nameof(property));
            var newUIProperty = new UIPropertyValue(queryExecutionContext.EntityRuntime, id, new UIPropertyPropertiesAvailableStatus());

            if (requestedProperties.Name)
            {
                newUIProperty.Name = property.Name;
            }

            if (requestedProperties.DisplayName)
            {
                newUIProperty.DisplayName = property.DisplayName;
            }

            if (requestedProperties.Description)
            {
                newUIProperty.Description = property.Description;
            }

            if (requestedProperties.ConfigurationIndependent)
            {
                newUIProperty.ConfigurationIndependent = !property.IsConfigurationDependent();
            }

            if (requestedProperties.IsReadOnly)
            {
                newUIProperty.IsReadOnly = property.ReadOnly;
            }

            if (requestedProperties.HelpUrl)
            {
                newUIProperty.HelpUrl = property.HelpUrl;
            }

            if (requestedProperties.CategoryName)
            {
                newUIProperty.CategoryName = property.Category;
            }

            if (requestedProperties.Order)
            {
                newUIProperty.Order = order;
            }

            if (requestedProperties.Type)
            {
                newUIProperty.Type = property switch
                {
                    IntProperty => "int",
                    BoolProperty => "bool",
                    EnumProperty => "enum",
                    DynamicEnumProperty => "enum",
                    StringListProperty => "list",
                    _ => "string"
                };
            }

            if (requestedProperties.SearchTerms)
            {
                string? searchTermsString = property.GetMetadataValueOrNull("SearchTerms");
                newUIProperty.SearchTerms = searchTermsString ?? string.Empty;
            }

            if (requestedProperties.DependsOn)
            {
                string? dependsOnString = property.GetMetadataValueOrNull("DependsOn");
                newUIProperty.DependsOn = dependsOnString ?? string.Empty;
            }

            if (requestedProperties.VisibilityCondition)
            {
                string? visibilityCondition = property.GetMetadataValueOrNull("VisibilityCondition");
                newUIProperty.VisibilityCondition = visibilityCondition ?? string.Empty;
            }

            ((IEntityValueFromProvider)newUIProperty).ProviderState = new PropertyProviderState(cache, property.ContainingRule, propertiesContext, property.Name);

            return newUIProperty;
        }

        public static IEnumerable<IEntityValue> CreateUIPropertyValues(IQueryExecutionContext queryExecutionContext, IEntityValue parent, IProjectState cache, QueryProjectPropertiesContext propertiesContext, Rule rule, IUIPropertyPropertiesAvailableStatus properties)
        {
            foreach ((int index, BaseProperty property) in rule.Properties.WithIndices())
            {
                if (property.Visible)
                {
                    IEntityValue propertyValue = CreateUIPropertyValue(queryExecutionContext, parent, cache, propertiesContext, property, index, properties);
                    yield return propertyValue;
                }
            }
        }

        public static async Task<IEntityValue?> CreateUIPropertyValueAsync(
            IQueryExecutionContext queryExecutionContext,
            EntityIdentity requestId,
            IProjectService2 projectService,
            IPropertyPageQueryCacheProvider queryCacheProvider,
            QueryProjectPropertiesContext propertiesContext,
            string propertyPageName,
            string propertyName,
            IUIPropertyPropertiesAvailableStatus requestedProperties)
        {
            if (projectService.GetLoadedProject(propertiesContext.File) is UnconfiguredProject project)
            {
                project.GetQueryDataVersion(out string versionKey, out long versionNumber);
                queryExecutionContext.ReportInputDataVersion(versionKey, versionNumber);

                if (await project.GetProjectLevelPropertyPagesCatalogAsync() is IPropertyPagesCatalog projectCatalog
                    && projectCatalog.GetSchema(propertyPageName) is Rule rule
                    && rule.TryGetPropertyAndIndex(propertyName, out BaseProperty? property, out int index)
                    && property.Visible)
                {
                    IProjectState cache = queryCacheProvider.CreateCache(project);
                    IEntityValue propertyValue = CreateUIPropertyValue(queryExecutionContext, requestId, cache, propertiesContext, property, index, requestedProperties);
                    return propertyValue;
                }
            }

            return null;
        }
    }
}
