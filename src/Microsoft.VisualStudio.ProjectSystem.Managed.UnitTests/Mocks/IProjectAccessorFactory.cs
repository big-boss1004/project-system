﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectAccessorFactory
    {
        public static IProjectAccessor Create(string xml = null)
        {
            var rootElement = ProjectRootElementFactory.Create(xml);

            return Create(rootElement);
        }

        public static IProjectAccessor Create(ProjectRootElement rootElement)
        {
            var evaluationProject = ProjectFactory.Create(rootElement);

            return new ProjectAccessor(rootElement, evaluationProject);
        }

        private class ProjectAccessor : IProjectAccessor
        {
            private readonly ProjectRootElement _rootElement;
            private readonly Project _evaluationProject;

            public ProjectAccessor(ProjectRootElement element, Project project)
            {
                _rootElement = element;
                _evaluationProject = project;
            }

            public Task<TResult> OpenProjectForReadAsync<TResult>(ConfiguredProject project, Func<Project, TResult> action, CancellationToken cancellationToken = default(CancellationToken))
            {
                var result = action(_evaluationProject);

                return Task.FromResult(result);
            }

            public Task<TResult> OpenProjectXmlForReadAsync<TResult>(UnconfiguredProject project, Func<ProjectRootElement, TResult> action, CancellationToken cancellationToken = default(CancellationToken))
            {
                var result = action(_rootElement);

                return Task.FromResult(result);
            }

            public Task OpenProjectXmlForWriteAsync(UnconfiguredProject project, Action<ProjectRootElement> action, CancellationToken cancellationToken = default(CancellationToken))
            {
                action(_rootElement);

                return Task.CompletedTask;
            }
        }
    }
}
