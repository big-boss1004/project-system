﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Build;

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    /// <summary>
    /// Provides "TargetFramework" project configuration dimension and values.
    /// </summary>
    /// <remarks>
    /// The Order attribute will determine the order of the dimensions inside the configuration
    /// service. We want Configuration|Platform|TargetFramework as the defaults so the values
    /// start at MaxValue and get decremented for each in order for future extenders to fall
    /// below these 3 providers.
    /// </remarks>
    [Export(typeof(IProjectConfigurationDimensionsProvider))]
    [Export(typeof(IActiveConfiguredProjectsDimensionProvider))]
    [AppliesTo(ProjectCapabilities.ProjectConfigurationsDeclaredDimensions)]
    [Order(DimensionProviderOrder.TargetFramework)]
    [ConfigurationDimensionDescription(ConfigurationGeneral.TargetFrameworkProperty, isVariantDimension: true)]
    internal class TargetFrameworkProjectConfigurationDimensionProvider : BaseProjectConfigurationDimensionProvider, IActiveConfiguredProjectsDimensionProvider
    {
        [ImportingConstructor]
        public TargetFrameworkProjectConfigurationDimensionProvider(IProjectAccessor projectAccessor)
            : base(projectAccessor, ConfigurationGeneral.TargetFrameworkProperty, ConfigurationGeneral.TargetFrameworksProperty)
        {
        }

        protected override async Task<ImmutableArray<string>> GetOrderedPropertyValuesAsync(UnconfiguredProject project)
        {
            Requires.NotNull(project, nameof(project));

            string targetFrameworksProperty = await GetPropertyValue(project, ConfigurationGeneral.TargetFrameworksProperty);
            if (targetFrameworksProperty != null)
            {
                return BuildUtilities.GetPropertyValues(targetFrameworksProperty).ToImmutableArray();
            }
            else
            {
                // If the project doesn't have a "TargetFrameworks" property, then this is not a cross-targeting project and we don't need a target framework dimension.
                return ImmutableArray<string>.Empty;
            }
        }

        public override Task OnDimensionValueChangedAsync(ProjectConfigurationDimensionValueChangedEventArgs args)
        {
            // CPS cannot change TargetFramework currently so don't do anything for the dimension
            return Task.CompletedTask;
        }
    }
}
