﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.GraphModel;

#nullable enable

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes
{
    /// <summary>
    /// Contains graph node ids and properties for Dependencies nodes
    /// </summary>
    internal static class DependenciesGraphSchema
    {
        public static readonly GraphSchema Schema = new GraphSchema("Microsoft.VisualStudio.ProjectSystem.VS.Tree.DependenciesSchema");
        public static readonly GraphCategory CategoryDependency = Schema.Categories.AddNewCategory(VSResources.GraphNodeCategoryDependency);

        private const string DependencyIdPropertyId = "Dependency.Id";
        public static readonly GraphProperty DependencyIdProperty;

        private const string ResolvedPropertyId = "Dependency.Resolved";
        public static readonly GraphProperty ResolvedProperty;

        private const string IsFrameworkAssemblyFolderPropertyId = "Dependency.IsFrameworkAssembly";
        public static readonly GraphProperty IsFrameworkAssemblyFolderProperty;

        static DependenciesGraphSchema()
        {
            ResolvedProperty = Schema.Properties.AddNewProperty(ResolvedPropertyId, typeof(bool));
            DependencyIdProperty = Schema.Properties.AddNewProperty(DependencyIdPropertyId, typeof(string));
            IsFrameworkAssemblyFolderProperty = Schema.Properties.AddNewProperty(IsFrameworkAssemblyFolderPropertyId, typeof(bool));
        }
    }
}
