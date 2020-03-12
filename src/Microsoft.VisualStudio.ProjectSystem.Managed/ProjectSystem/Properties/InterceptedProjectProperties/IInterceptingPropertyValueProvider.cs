﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// A project property provider that intercepts all the callbacks for a specific property name
    /// on the default <see cref="IProjectPropertiesProvider"/> for validation and/or transformation of the property value.
    /// </summary>
    internal interface IInterceptingPropertyValueProvider
    {
        /// <summary>
        /// Validate and/or transform the given evaluated property value.
        /// </summary>
        Task<string> OnGetEvaluatedPropertyValueAsync(string evaluatedPropertyValue, IProjectProperties defaultProperties);

        /// <summary>
        /// Validate and/or transform the given unevaluated property value, i.e. "raw" value read from the project file.
        /// </summary>
        Task<string> OnGetUnevaluatedPropertyValueAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties);

        /// <summary>
        /// Validate and/or transform the given unevaluated property value to be written back to the project file.
        /// </summary>
        Task<string?> OnSetPropertyValueAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null);
    }
}
