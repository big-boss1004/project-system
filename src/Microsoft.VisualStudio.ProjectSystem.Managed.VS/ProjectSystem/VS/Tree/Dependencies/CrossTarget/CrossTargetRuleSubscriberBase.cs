﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    internal abstract class CrossTargetRuleSubscriberBase<T> : OnceInitializedOnceDisposedAsync, ICrossTargetSubscriber where T : IRuleChangeContext
    {
#pragma warning disable CA2213 // OnceInitializedOnceDisposedAsync are not tracked corretly by the IDisposeable analyzer
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(initialCount: 1);
#pragma warning restore CA2213
        private readonly List<IDisposable> _evaluationSubscriptionLinks;
        private readonly List<IDisposable> _designTimeBuildSubscriptionLinks;
        private readonly IUnconfiguredProjectCommonServices _commonServices;
        private readonly IProjectAsynchronousTasksService _tasksService;
        private readonly IDependencyTreeTelemetryService _treeTelemetryService;
        private ICrossTargetSubscriptionsHost _host;
        private AggregateCrossTargetProjectContext _currentProjectContext;

        public CrossTargetRuleSubscriberBase(
            IUnconfiguredProjectCommonServices commonServices,
            IProjectAsynchronousTasksService tasksService,
            IDependencyTreeTelemetryService treeTelemetryService)
            : base(commonServices.ThreadingService.JoinableTaskContext)
        {
            _commonServices = commonServices;
            _tasksService = tasksService;
            _treeTelemetryService = treeTelemetryService;
            _evaluationSubscriptionLinks = new List<IDisposable>();
            _designTimeBuildSubscriptionLinks = new List<IDisposable>();
        }

        protected abstract OrderPrecedenceImportCollection<ICrossTargetRuleHandler<T>> Handlers { get; }

        public async Task InitializeSubscriberAsync(ICrossTargetSubscriptionsHost host, IProjectSubscriptionService subscriptionService)
        {
            _host = host;

            await InitializeAsync().ConfigureAwait(false);

            IEnumerable<string> watchedEvaluationRules = GetWatchedRules(RuleHandlerType.Evaluation);
            IEnumerable<string> watchedDesignTimeBuildRules = GetWatchedRules(RuleHandlerType.DesignTimeBuild);

            SubscribeToConfiguredProject(
                _commonServices.ActiveConfiguredProject, subscriptionService, watchedEvaluationRules, watchedDesignTimeBuildRules);
        }

        public Task AddSubscriptionsAsync(AggregateCrossTargetProjectContext newProjectContext)
        {
            Requires.NotNull(newProjectContext, nameof(newProjectContext));

            _currentProjectContext = newProjectContext;

            IEnumerable<string> watchedEvaluationRules = GetWatchedRules(RuleHandlerType.Evaluation);
            IEnumerable<string> watchedDesignTimeBuildRules = GetWatchedRules(RuleHandlerType.DesignTimeBuild);

            // initialize telemetry with all rules for each target framework
            foreach (ITargetedProjectContext projectContext in newProjectContext.InnerProjectContexts)
            {
                _treeTelemetryService.InitializeTargetFrameworkRules(projectContext.TargetFramework, watchedEvaluationRules);
                _treeTelemetryService.InitializeTargetFrameworkRules(projectContext.TargetFramework, watchedDesignTimeBuildRules);
            }

            foreach (ConfiguredProject configuredProject in newProjectContext.InnerConfiguredProjects)
            {
                SubscribeToConfiguredProject(
                    configuredProject, configuredProject.Services.ProjectSubscription, watchedEvaluationRules, watchedDesignTimeBuildRules);
            }

            return Task.CompletedTask;
        }

        public Task ReleaseSubscriptionsAsync()
        {
            _currentProjectContext = null;

            foreach (IDisposable link in _evaluationSubscriptionLinks.Concat(_designTimeBuildSubscriptionLinks))
            {
                link.Dispose();
            }

            _evaluationSubscriptionLinks.Clear();
            _designTimeBuildSubscriptionLinks.Clear();

            return Task.CompletedTask;
        }

        public async Task OnContextReleasedAsync(ITargetedProjectContext innerContext)
        {
            foreach (Lazy<ICrossTargetRuleHandler<T>, IOrderPrecedenceMetadataView> handler in Handlers)
            {
                await handler.Value.OnContextReleasedAsync(innerContext).ConfigureAwait(false);
            }
        }

        private void SubscribeToConfiguredProject(
            ConfiguredProject configuredProject,
            IProjectSubscriptionService subscriptionService,
            IEnumerable<string> watchedEvaluationRules,
            IEnumerable<string> watchedDesignTimeBuildRules)
        {
            var intermediateBlockDesignTime =
                new BufferBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(
                    new ExecutionDataflowBlockOptions()
                    {
                        NameFormat = "CrossTarget Intermediate DesignTime Input: {1}"
                    });

            var intermediateBlockEvaluation =
                new BufferBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(
                    new ExecutionDataflowBlockOptions()
                    {
                        NameFormat = "CrossTarget Intermediate Evaluation Input: {1}"
                    });

            _designTimeBuildSubscriptionLinks.Add(
                subscriptionService.JointRuleSource.SourceBlock.LinkTo(
                    intermediateBlockDesignTime,
                  ruleNames: watchedDesignTimeBuildRules.Union(watchedEvaluationRules),
                  suppressVersionOnlyUpdates: true,
                  linkOptions: DataflowOption.PropagateCompletion));

            _evaluationSubscriptionLinks.Add(
                subscriptionService.ProjectRuleSource.SourceBlock.LinkTo(
                    intermediateBlockEvaluation,
                    ruleNames: watchedEvaluationRules,
                    suppressVersionOnlyUpdates: true,
                    linkOptions: DataflowOption.PropagateCompletion));

            var actionBlockDesignTimeBuild =
                new ActionBlock<IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectCatalogSnapshot, IProjectCapabilitiesSnapshot>>>(
                    e => OnProjectChangedAsync(e, configuredProject, RuleHandlerType.DesignTimeBuild),
                    new ExecutionDataflowBlockOptions()
                    {
                        NameFormat = "CrossTarget DesignTime Input: {1}"
                    });

            var actionBlockEvaluation =
                new ActionBlock<IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectCatalogSnapshot, IProjectCapabilitiesSnapshot>>>(
                     e => OnProjectChangedAsync(e, configuredProject, RuleHandlerType.Evaluation),
                     new ExecutionDataflowBlockOptions()
                     {
                         NameFormat = "CrossTarget Evaluation Input: {1}"
                     });

            _designTimeBuildSubscriptionLinks.Add(ProjectDataSources.SyncLinkTo(
                intermediateBlockDesignTime.SyncLinkOptions(),
                subscriptionService.ProjectCatalogSource.SourceBlock.SyncLinkOptions(),
                configuredProject.Capabilities.SourceBlock.SyncLinkOptions(),
                actionBlockDesignTimeBuild,
                linkOptions: DataflowOption.PropagateCompletion));

            _evaluationSubscriptionLinks.Add(ProjectDataSources.SyncLinkTo(
                intermediateBlockEvaluation.SyncLinkOptions(),
                subscriptionService.ProjectCatalogSource.SourceBlock.SyncLinkOptions(),
                configuredProject.Capabilities.SourceBlock.SyncLinkOptions(),
                actionBlockEvaluation,
                linkOptions: DataflowOption.PropagateCompletion));
        }

        private IEnumerable<string> GetWatchedRules(RuleHandlerType handlerType)
        {
            IEnumerable<Lazy<ICrossTargetRuleHandler<T>, IOrderPrecedenceMetadataView>> supportedHandler = Handlers.Where(h => h.Value.SupportsHandlerType(handlerType));
            var uniqueRuleNames = new HashSet<string>(StringComparers.RuleNames);
            foreach (Lazy<ICrossTargetRuleHandler<T>, IOrderPrecedenceMetadataView> handler in supportedHandler)
            {
                foreach (string ruleName in handler.Value.GetRuleNames(handlerType))
                {
                    uniqueRuleNames.Add(ruleName);
                }
            }
            return uniqueRuleNames;
        }

        private async Task OnProjectChangedAsync(
            IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectCatalogSnapshot, IProjectCapabilitiesSnapshot>> e,
            ConfiguredProject configuredProject,
            RuleHandlerType handlerType)
        {
            if (IsDisposing || IsDisposed)
            {
                return;
            }

            await _tasksService.LoadedProjectAsync(async () =>
            {
                if (_tasksService.UnloadCancellationToken.IsCancellationRequested)
                {
                    return;
                }

                using (ProjectCapabilitiesContext.CreateIsolatedContext(configuredProject, e.Value.Item3))
                {
                    await HandleAsync(e, handlerType).ConfigureAwait(false);
                }
            });
        }

        private async Task HandleAsync(
                    IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectCatalogSnapshot, IProjectCapabilitiesSnapshot>> e,
                    RuleHandlerType handlerType)
        {
            AggregateCrossTargetProjectContext currentAggregateContext = await _host.GetCurrentAggregateProjectContext().ConfigureAwait(false);
            if (currentAggregateContext == null || _currentProjectContext != currentAggregateContext)
            {
                return;
            }

            IProjectSubscriptionUpdate update = e.Value.Item1;
            IProjectCatalogSnapshot catalogs = e.Value.Item2;
            IEnumerable<ICrossTargetRuleHandler<T>> handlers = Handlers.Select(h => h.Value)
                                   .Where(h => h.SupportsHandlerType(handlerType));

            // We need to process the update within a lock to ensure that we do not release this context during processing.
            // TODO: Enable concurrent execution of updates themeselves, i.e. two separate invocations of HandleAsync
            //       should be able to run concurrently.
            using (await _gate.DisposableWaitAsync().ConfigureAwait(true))
            {
                // Get the inner workspace project context to update for this change.
                ITargetedProjectContext projectContextToUpdate = currentAggregateContext
                    .GetInnerProjectContext(update.ProjectConfiguration, out bool isActiveContext);
                if (projectContextToUpdate == null)
                {
                    return;
                }

                // Broken design time builds sometimes cause updates with no project changes and sometimes
                // cause updates with a project change that has no difference.
                // We handle the former case here, and the latter case is handled in the CommandLineItemHandler.
                if (update.ProjectChanges.Count == 0)
                {
                    if (handlerType == RuleHandlerType.DesignTimeBuild)
                    {
                        projectContextToUpdate.LastDesignTimeBuildSucceeded = false;
                    }

                    return;
                }

                T ruleChangeContext = CreateRuleChangeContext(
                    currentAggregateContext.ActiveProjectContext.TargetFramework, catalogs);
                foreach (ICrossTargetRuleHandler<T> handler in handlers)
                {
                    ImmutableDictionary<string, IProjectChangeDescription>.Builder builder = ImmutableDictionary.CreateBuilder<string, IProjectChangeDescription>(StringComparers.RuleNames);
                    ImmutableHashSet<string> handlerRules = handler.GetRuleNames(handlerType);
                    builder.AddRange(update.ProjectChanges.Where(
                        x => handlerRules.Contains(x.Key)));
                    ImmutableDictionary<string, IProjectChangeDescription> projectChanges = builder.ToImmutable();

                    if (handler.ReceiveUpdatesWithEmptyProjectChange
                        || projectChanges.Any(x => x.Value.Difference.AnyChanges))
                    {
                        await handler.HandleAsync(e,
                                                  projectChanges,
                                                  projectContextToUpdate,
                                                  isActiveContext,
                                                  ruleChangeContext)
                                     .ConfigureAwait(true);
                    }
                }

                await CompleteHandleAsync(ruleChangeContext).ConfigureAwait(false);

                // record all the rules that have occurred
                _treeTelemetryService.ObserveTargetFrameworkRules(projectContextToUpdate.TargetFramework, update.ProjectChanges.Keys);
            }
        }

        protected abstract T CreateRuleChangeContext(ITargetFramework target, IProjectCatalogSnapshot catalogs);

        protected virtual Task CompleteHandleAsync(T ruleChangeContext)
        {
            return Task.CompletedTask;
        }

        protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            if (initialized)
            {
                await ReleaseSubscriptionsAsync().ConfigureAwait(false);
            }
        }
    }
}
