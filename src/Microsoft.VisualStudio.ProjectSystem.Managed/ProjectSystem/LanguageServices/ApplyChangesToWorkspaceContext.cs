﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Applies <see cref="IProjectVersionedValue{T}"/> values to a <see cref="IWorkspaceProjectContext"/>.
    /// </summary>
    /// <remarks>
    ///     This class is not thread-safe and it is up to callers to prevent overlapping of calls to
    ///     <see cref="ApplyProjectBuild(IProjectVersionedValue{IProjectSubscriptionUpdate}, CommandLineArgumentsSnapshot, ContextState, CancellationToken)"/>,
    ///     <see cref="ApplyProjectEvaluation(IProjectVersionedValue{IProjectSubscriptionUpdate}, ContextState, CancellationToken)"/> and
    ///     <see cref="ApplySourceItems(IProjectVersionedValue{IProjectSubscriptionUpdate}, ContextState, CancellationToken)"/>
    /// </remarks>
    [Export(typeof(IApplyChangesToWorkspaceContext))]
    internal class ApplyChangesToWorkspaceContext : OnceInitializedOnceDisposed, IApplyChangesToWorkspaceContext
    {
        private const string ProjectBuildRuleName = CompilerCommandLineArgs.SchemaName;
        private readonly ConfiguredProject _project;
        private readonly IProjectDiagnosticOutputService _logger;
        private readonly ExportFactory<IWorkspaceContextHandler>[] _workspaceContextHandlerFactories;
        private IWorkspaceProjectContext? _context;
        private ExportLifetimeContext<IWorkspaceContextHandler>[] _handlers = Array.Empty<ExportLifetimeContext<IWorkspaceContextHandler>>();

        [ImportingConstructor]
        public ApplyChangesToWorkspaceContext(ConfiguredProject project, IProjectDiagnosticOutputService logger, [ImportMany] ExportFactory<IWorkspaceContextHandler>[] workspaceContextHandlerFactories)
        {
            _project = project;
            _logger = logger;
            _workspaceContextHandlerFactories = workspaceContextHandlerFactories;

            CommandLineParsers = new OrderPrecedenceImportCollection<ICommandLineParserService>(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<ICommandLineParserService> CommandLineParsers { get; }

        public void Initialize(IWorkspaceProjectContext context)
        {
            Requires.NotNull(context, nameof(context));

            if (_context != null)
                throw new InvalidOperationException("Already initialized.");

            _context = context;

            EnsureInitialized();
        }

        public void ApplyProjectBuild(
            IProjectVersionedValue<IProjectSubscriptionUpdate> update,
            CommandLineArgumentsSnapshot commandLineArgumentsSnapshot,
            ContextState state,
            CancellationToken cancellationToken)
        {
            Requires.NotNull(update, nameof(update));
            Requires.NotNull(commandLineArgumentsSnapshot, nameof(commandLineArgumentsSnapshot));

            VerifyInitializedAndNotDisposed();

            IProjectChangeDescription projectChange = update.Value.ProjectChanges[ProjectBuildRuleName];

            if (projectChange.Difference.AnyChanges)
            {
                IComparable version = GetConfiguredProjectVersion(update);

                Assumes.NotNull(_context);

                // We just need to pass all options to Roslyn
                _context.SetOptions(commandLineArgumentsSnapshot.Arguments);

                ProcessCommandLine(version, projectChange.Difference, state, cancellationToken);
                ProcessProjectBuildFailure(projectChange.After);
            }
        }

        public void ApplyProjectEvaluation(IProjectVersionedValue<IProjectSubscriptionUpdate> update, ContextState state, CancellationToken cancellationToken)
        {
            Requires.NotNull(update, nameof(update));

            VerifyInitializedAndNotDisposed();

            IComparable version = GetConfiguredProjectVersion(update);

            ProcessProjectEvaluationHandlers(version, update, state, cancellationToken);
        }

        public void ApplySourceItems(IProjectVersionedValue<IProjectSubscriptionUpdate> update, ContextState state, CancellationToken cancellationToken)
        {
            Requires.NotNull(update, nameof(update));

            VerifyInitializedAndNotDisposed();

            IComparable version = GetConfiguredProjectVersion(update);

            ProcessSourceItemsHandlers(version, update, state, cancellationToken);
        }

        public IEnumerable<string> GetProjectEvaluationRules()
        {
            VerifyInitializedAndNotDisposed();

            return _handlers.Select(e => e.Value)
                            .OfType<IProjectEvaluationHandler>()
                            .Select(e => e.ProjectEvaluationRule)
                            .Distinct(StringComparers.RuleNames)
                            .ToArray();
        }

        public IEnumerable<string> GetProjectBuildRules()
        {
            VerifyInitializedAndNotDisposed();

            return new string[] { ProjectBuildRuleName };
        }

        protected override void Dispose(bool disposing)
        {
            foreach (ExportLifetimeContext<IWorkspaceContextHandler> handler in _handlers)
            {
                handler.Dispose();
            }

            _context = null;
            _handlers = Array.Empty<ExportLifetimeContext<IWorkspaceContextHandler>>();
        }

        protected override void Initialize()
        {
            Assumes.NotNull(_context);

            _handlers = _workspaceContextHandlerFactories.SelectArray(h => h.CreateExport());

            foreach (ExportLifetimeContext<IWorkspaceContextHandler> handler in _handlers)
            {
                handler.Value.Initialize(_context);
            }
        }

        private void ProcessProjectBuildFailure(IProjectRuleSnapshot snapshot)
        {
            Assumes.NotNull(_context);

            // If 'CompileDesignTime' didn't run due to a preceding failed target, or a failure in itself, IsEvaluationSucceeded returns false.
            //
            // We still forward those 'removes' of references, sources, etc onto Roslyn to avoid duplicate/incorrect results when the next
            // successful build occurs, because it will be diff between it and this failed build.

            bool succeeded = snapshot.IsEvaluationSucceeded();

            if (_context.LastDesignTimeBuildSucceeded != succeeded)
            {
                _logger.WriteLine(succeeded ? "Last design-time build succeeded, turning semantic errors back on." : "Last design-time build failed, turning semantic errors off.");
                _context.LastDesignTimeBuildSucceeded = succeeded;
            }
        }

        private void ProcessCommandLine(IComparable version, IProjectChangeDiff differences, ContextState state, CancellationToken cancellationToken)
        {
            ICommandLineParserService? parser = CommandLineParsers.FirstOrDefault()?.Value;

            Assumes.Present(parser);

            string baseDirectory = Path.GetDirectoryName(_project.UnconfiguredProject.FullPath);

            BuildOptions added = parser.Parse(differences.AddedItems, baseDirectory);
            BuildOptions removed = parser.Parse(differences.RemovedItems, baseDirectory);

            ProcessCommandLineHandlers(version, added, removed, state, cancellationToken);
        }

        private void ProcessCommandLineHandlers(IComparable version, BuildOptions added, BuildOptions removed, ContextState state, CancellationToken cancellationToken)
        {
            foreach (ExportLifetimeContext<IWorkspaceContextHandler> handler in _handlers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (handler.Value is ICommandLineHandler commandLineHandler)
                {
                    commandLineHandler.Handle(version, added, removed, state, _logger);
                }
            }
        }

        private void ProcessProjectEvaluationHandlers(IComparable version, IProjectVersionedValue<IProjectSubscriptionUpdate> update, ContextState state, CancellationToken cancellationToken)
        {
            foreach (ExportLifetimeContext<IWorkspaceContextHandler> handler in _handlers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (handler.Value is IProjectEvaluationHandler evaluationHandler)
                {
                    IProjectChangeDescription projectChange = update.Value.ProjectChanges[evaluationHandler.ProjectEvaluationRule];
                    if (!projectChange.Difference.AnyChanges)
                        continue;

                    evaluationHandler.Handle(version, projectChange, state, _logger);
                }
            }
        }

        private void ProcessSourceItemsHandlers(IComparable version, IProjectVersionedValue<IProjectSubscriptionUpdate> update, ContextState state, CancellationToken cancellationToken)
        {
            foreach (ExportLifetimeContext<IWorkspaceContextHandler> handler in _handlers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (handler.Value is ISourceItemsHandler sourceItemsHandler)
                {
                    sourceItemsHandler.Handle(version, update.Value.ProjectChanges, state, _logger);
                }
            }
        }

        private static IComparable GetConfiguredProjectVersion(IProjectVersionedValue<IProjectSubscriptionUpdate> update)
        {
            return update.DataSourceVersions[ProjectDataSources.ConfiguredProjectVersion];
        }

        private void VerifyInitializedAndNotDisposed()
        {
            VerifyNotDisposed();
            VerifyInitialized();
        }

        private void VerifyInitialized()
        {
            Verify.Operation(IsInitialized, "Must call Initialize(IWorkspaceProjectContext) first.");
        }

        private void VerifyNotDisposed()
        {
            Verify.NotDisposed(this);
        }
    }
}
