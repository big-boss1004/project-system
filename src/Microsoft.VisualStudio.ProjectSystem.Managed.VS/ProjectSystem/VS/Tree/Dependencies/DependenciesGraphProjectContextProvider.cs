﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.VS.Extensibility;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Global scope contract that provides information about project level 
    /// dependencies graph contexts.
    /// </summary>
    [Export(typeof(IDependenciesGraphProjectContextProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class DependenciesGraphProjectContextProvider : IDependenciesGraphProjectContextProvider
    {
        [ImportingConstructor]
        public DependenciesGraphProjectContextProvider(IProjectExportProvider projectExportProvider, 
                                                       SVsServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            ProjectExportProvider = projectExportProvider;
        }

        private IProjectExportProvider ProjectExportProvider { get; }

        private SVsServiceProvider ServiceProvider { get; }

        private ConcurrentDictionary<string, IDependenciesGraphProjectContext> ProjectContexts { get; }
                            = new ConcurrentDictionary<string, IDependenciesGraphProjectContext>
                                        (StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// When project context changed event received from any context, send a global level
        /// "context changed" event, to notify  <see cref="DependenciesGraphProvider"/>.
        /// </summary>
        internal void OnProjectContextChanged(object sender, ProjectContextEventArgs e)
        {
            var context = e.Context;
            if (context == null)
            {
                return;
            }

            ProjectContextChanged?.Invoke(this, new ProjectContextEventArgs(context));
        }

        /// <summary>
        /// When a given project context is unloaded, remove it form the cache and unregister event handlers
        /// </summary>
        internal void OnProjectContextUnloaded(object sender, ProjectContextEventArgs e)
        {
            var context = e.Context;
            if (context == null)
            {
                return;
            }

            ProjectContextUnloaded?.Invoke(this, new ProjectContextEventArgs(context));

            // Remove context for the unloaded project from the cache
            IDependenciesGraphProjectContext removedContext;
            ProjectContexts.TryRemove(context.ProjectFilePath, out removedContext);

            context.ProjectContextChanged -= OnProjectContextChanged;
            context.ProjectContextUnloaded -= OnProjectContextUnloaded;
        }

        /// <summary>
        /// Returns an unconfigured project level contexts for given project file path.
        /// </summary>
        /// <param name="projectFilePath">Full path to project path.</param>
        /// <returns>
        /// Instance of <see cref="IDependenciesGraphProjectContext"/> or null if context was not found for given project file.
        /// </returns>
        public IDependenciesGraphProjectContext GetProjectContext(string projectFilePath)
        {
            if (string.IsNullOrEmpty(projectFilePath))
            {
                throw new ArgumentException(nameof(projectFilePath));
            }

            IDependenciesGraphProjectContext context = null;
            if (ProjectContexts.TryGetValue(projectFilePath, out context))
            {
                return context;
            }

            context = ProjectExportProvider.GetExport<IDependenciesGraphProjectContext>(projectFilePath);
            if (context == null)
            {
                return null;
            }

            ProjectContexts[projectFilePath] = context;
            context.ProjectContextChanged += OnProjectContextChanged;
            context.ProjectContextUnloaded += OnProjectContextUnloaded;

            return context;
        }

        public IEnumerable<IDependenciesGraphProjectContext> GetProjectContexts()
        {
            var projectService = ServiceProvider.GetProjectService();
            if (projectService == null)
            {
                return null;
            }

            return GetProjectContextsInternal(projectService);
        }

        internal IEnumerable<IDependenciesGraphProjectContext> GetProjectContextsInternal(
                    IProjectService projectService)
        {
            var projects = projectService.LoadedUnconfiguredProjects;
            foreach (var project in projects)
            {
                var context = GetProjectContext(project.FullPath);
                if (context != null)
                {
                    yield return context;
                }
            }
        }

        /// <summary>
        /// Gets called when context (projec dependencies) change
        /// </summary>
        public event EventHandler<ProjectContextEventArgs> ProjectContextChanged;

        /// <summary>
        /// Gets called when project unloads to notify GraphProvider to release
        /// any data associated with the project.
        /// </summary>
        public event EventHandler<ProjectContextEventArgs> ProjectContextUnloaded;
    }
}
