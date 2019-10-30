﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    internal sealed partial class BuildUpToDateCheck
    {
        internal sealed class State
        {
            public static State Empty { get; } = new State();

            public string? MSBuildProjectFullPath { get; }
            public string? MSBuildProjectDirectory { get; }
            public string? MarkerFile { get; }
            public string? OutputRelativeOrFullPath { get; }
            public string? NewestImportInput { get; }
            public IComparable? LastVersionSeen { get; }
            public bool IsDisabled { get; }

            /// <summary>
            /// Gets the time at which the set of items changed.
            /// </summary>
            /// <remarks>
            /// <para>
            /// This is not the last timestamp of the items themselves. It is time at which items were
            /// last added or removed from the project.
            /// </para>
            /// <para>
            /// This property is not updated until after the first query occurs. Until that time it will
            /// equal <see cref="DateTime.MinValue"/> which represents the fact that we do not know when
            /// the set of items was last changed, so we cannot base any decisions on this data property.
            /// </para>
            /// </remarks>
            public DateTime LastItemsChangedAtUtc { get; }

            /// <summary>
            /// Gets the time at which the last up-to-date check was made.
            /// </summary>
            /// <remarks>
            /// This value is required in order to protect against a race condition described in
            /// https://github.com/dotnet/project-system/issues/4014. Specifically, if source files are
            /// modified during a compilation, but before that compilation's outputs are produced, then
            /// the changed input file's timestamp will be earlier than the compilation output, making
            /// it seem as though the compilation is up to date when in fact the input was not included
            /// in that compilation. We use this property as a proxy for compilation start time, whereas
            /// the outputs represent compilation end time.
            /// </remarks>
            public DateTime LastCheckedAtUtc { get; }

            public ImmutableHashSet<string> ItemTypes { get; }
            public ImmutableDictionary<string, ImmutableHashSet<(string path, string? link, CopyToOutputDirectoryType copyType)>> ItemsByItemType { get; }
            public ImmutableHashSet<string> CustomInputs { get; }
            public ImmutableHashSet<string> CustomOutputs { get; }
            public ImmutableHashSet<string> BuiltOutputs { get; }

            /// <summary>Key is destination, value is source.</summary>
            public ImmutableDictionary<string, string> CopiedOutputFiles { get; }

            public ImmutableHashSet<string> AnalyzerReferences { get; }
            public ImmutableHashSet<string> CompilationReferences { get; }
            public ImmutableHashSet<string> CopyReferenceInputs { get; }

            private State()
            {
                var emptyPathSet = ImmutableHashSet.Create(StringComparers.Paths);

                LastItemsChangedAtUtc = DateTime.MinValue;
                LastCheckedAtUtc = DateTime.MinValue;
                ItemTypes = ImmutableHashSet.Create(StringComparers.ItemTypes);
                ItemsByItemType = ImmutableDictionary.Create<string, ImmutableHashSet<(string path, string? link, CopyToOutputDirectoryType copyType)>>(StringComparers.ItemTypes);
                CustomInputs = emptyPathSet;
                CustomOutputs = emptyPathSet;
                BuiltOutputs = emptyPathSet;
                CopiedOutputFiles = ImmutableDictionary.Create<string, string>(StringComparers.Paths);
                AnalyzerReferences = emptyPathSet;
                CompilationReferences = emptyPathSet;
                CopyReferenceInputs = emptyPathSet;
            }

            private State(
                string? msBuildProjectFullPath,
                string? msBuildProjectDirectory,
                string? markerFile,
                string? outputRelativeOrFullPath,
                string? newestImportInput,
                IComparable? lastVersionSeen,
                bool isDisabled,
                ImmutableHashSet<string> itemTypes,
                ImmutableDictionary<string, ImmutableHashSet<(string, string?, CopyToOutputDirectoryType)>> itemsByItemType,
                ImmutableHashSet<string> customInputs,
                ImmutableHashSet<string> customOutputs,
                ImmutableHashSet<string> builtOutputs,
                ImmutableDictionary<string, string> copiedOutputFiles,
                ImmutableHashSet<string> analyzerReferences,
                ImmutableHashSet<string> compilationReferences,
                ImmutableHashSet<string> copyReferenceInputs,
                DateTime lastItemsChangedAtUtc,
                DateTime lastCheckedAtUtc)
            {
                MSBuildProjectFullPath = msBuildProjectFullPath;
                MSBuildProjectDirectory = msBuildProjectDirectory;
                MarkerFile = markerFile;
                OutputRelativeOrFullPath = outputRelativeOrFullPath;
                NewestImportInput = newestImportInput;
                LastVersionSeen = lastVersionSeen;
                IsDisabled = isDisabled;
                ItemTypes = itemTypes;
                ItemsByItemType = itemsByItemType;
                CustomInputs = customInputs;
                CustomOutputs = customOutputs;
                BuiltOutputs = builtOutputs;
                CopiedOutputFiles = copiedOutputFiles;
                AnalyzerReferences = analyzerReferences;
                CompilationReferences = compilationReferences;
                CopyReferenceInputs = copyReferenceInputs;
                LastItemsChangedAtUtc = lastItemsChangedAtUtc;
                LastCheckedAtUtc = lastCheckedAtUtc;
            }

            public State Update(
                IProjectSubscriptionUpdate jointRuleUpdate,
                IProjectSubscriptionUpdate sourceItemsUpdate,
                IProjectItemSchema projectItemSchema,
                IComparable configuredProjectVersion)
            {
                bool isDisabled = jointRuleUpdate.CurrentState.IsPropertyTrue(ConfigurationGeneral.SchemaName, ConfigurationGeneral.DisableFastUpToDateCheckProperty, defaultValue: false);

                string? msBuildProjectFullPath = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildProjectFullPathProperty, MSBuildProjectFullPath);
                string? msBuildProjectDirectory = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildProjectDirectoryProperty, MSBuildProjectDirectory);
                string? outputRelativeOrFullPath = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.OutputPathProperty, OutputRelativeOrFullPath);
                string msBuildAllProjects = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildAllProjectsProperty, "");

                // The first item in this semicolon-separated list of project files will always be the one
                // with the newest timestamp. As we are only interested in timestamps on these files, we can
                // save memory and time by only considering this first path (dotnet/project-system#4333).
                string? newestImportInput = new LazyStringSplit(msBuildAllProjects, ';').FirstOrDefault();

                ImmutableHashSet<string> analyzerReferences;
                if (jointRuleUpdate.ProjectChanges.TryGetValue(ResolvedAnalyzerReference.SchemaName, out IProjectChangeDescription change) && change.Difference.AnyChanges)
                {
                    analyzerReferences = change.After.Items.Select(item => item.Value[ResolvedAnalyzerReference.ResolvedPathProperty]).ToImmutableHashSet(StringComparers.Paths);
                }
                else
                {
                    analyzerReferences = AnalyzerReferences;
                }

                ImmutableHashSet<string> customInputs;
                if (jointRuleUpdate.ProjectChanges.TryGetValue(UpToDateCheckInput.SchemaName, out change) && change.Difference.AnyChanges)
                {
                    customInputs = change.After.Items.Keys.ToImmutableHashSet(StringComparers.Paths);
                }
                else
                {
                    customInputs = CustomInputs;
                }

                ImmutableHashSet<string> customOutputs;
                if (sourceItemsUpdate.ProjectChanges.TryGetValue(UpToDateCheckOutput.SchemaName, out change) && change.Difference.AnyChanges)
                {
                    customOutputs = change.After.Items.Keys.ToImmutableHashSet(StringComparers.Paths);
                }
                else if (jointRuleUpdate.ProjectChanges.TryGetValue(UpToDateCheckOutput.SchemaName, out change) && change.Difference.AnyChanges)
                {
                    customOutputs = change.After.Items.Keys.ToImmutableHashSet(StringComparers.Paths);
                }
                else
                {
                    customOutputs = CustomOutputs;
                }

                string? markerFile;
                if (jointRuleUpdate.ProjectChanges.TryGetValue(CopyUpToDateMarker.SchemaName, out change) && change.Difference.AnyChanges)
                {
                    markerFile = change.After.Items.Count == 1 ? change.After.Items.Single().Key : null;
                }
                else
                {
                    markerFile = MarkerFile;
                }

                ImmutableHashSet<string> compilationReferences;
                ImmutableHashSet<string> copyReferenceInputs;
                if (jointRuleUpdate.ProjectChanges.TryGetValue(ResolvedCompilationReference.SchemaName, out change) && change.Difference.AnyChanges)
                {
                    ImmutableHashSet<string>.Builder compilationReferencesBuilder = ImmutableHashSet.CreateBuilder(StringComparers.Paths);
                    ImmutableHashSet<string>.Builder copyReferenceInputsBuilder = ImmutableHashSet.CreateBuilder(StringComparers.Paths);

                    foreach (IImmutableDictionary<string, string> item in change.After.Items.Values)
                    {
                        compilationReferencesBuilder.Add(item[ResolvedCompilationReference.ResolvedPathProperty]);
                        if (!string.IsNullOrWhiteSpace(item[CopyUpToDateMarker.SchemaName]))
                        {
                            copyReferenceInputsBuilder.Add(item[CopyUpToDateMarker.SchemaName]);
                        }

                        if (!string.IsNullOrWhiteSpace(item[ResolvedCompilationReference.OriginalPathProperty]))
                        {
                            copyReferenceInputsBuilder.Add(item[ResolvedCompilationReference.OriginalPathProperty]);
                        }
                    }

                    compilationReferences = compilationReferencesBuilder.ToImmutable();
                    copyReferenceInputs = copyReferenceInputsBuilder.ToImmutable();
                }
                else
                {
                    compilationReferences = CompilationReferences;
                    copyReferenceInputs = CopyReferenceInputs;
                }

                ImmutableHashSet<string> builtOutputs;
                ImmutableDictionary<string, string> copiedOutputFiles;
                if (jointRuleUpdate.ProjectChanges.TryGetValue(UpToDateCheckBuilt.SchemaName, out change) && change.Difference.AnyChanges)
                {
                    ImmutableHashSet<string>.Builder builtOutputsBuilder = ImmutableHashSet.CreateBuilder(StringComparers.Paths);
                    ImmutableDictionary<string, string>.Builder copiedOutputFilesBuilder = ImmutableDictionary.CreateBuilder<string, string>(StringComparers.Paths);

                    foreach ((string destination, IImmutableDictionary<string, string> properties) in change.After.Items)
                    {
                        if (properties.TryGetValue(UpToDateCheckBuilt.OriginalProperty, out string source) && !string.IsNullOrEmpty(source))
                        {
                            // This file is copied, not built
                            // Remember the `Original` source for later
                            copiedOutputFilesBuilder[destination] = source;
                        }
                        else
                        {
                            // This file is built, not copied
                            builtOutputsBuilder.Add(destination);
                        }
                    }

                    builtOutputs = builtOutputsBuilder.ToImmutable();
                    copiedOutputFiles = copiedOutputFilesBuilder.ToImmutable();
                }
                else
                {
                    builtOutputs = BuiltOutputs;
                    copiedOutputFiles = CopiedOutputFiles;
                }

                // TODO these are probably the same as the previous set, so merge them to avoid allocation
                var itemTypes = projectItemSchema.GetKnownItemTypes()
                                                 .Where(itemType => projectItemSchema.GetItemType(itemType).UpToDateCheckInput)
                                                 .ToImmutableHashSet(StringComparers.ItemTypes);

                ImmutableDictionary<string, ImmutableHashSet<(string path, string? link, CopyToOutputDirectoryType copyType)>>.Builder itemsByItemTypeBuilder;
                bool itemTypesChanged = !ItemTypes.SetEquals(itemTypes);

                if (itemTypesChanged)
                {
                    itemsByItemTypeBuilder = ImmutableDictionary.CreateBuilder<string, ImmutableHashSet<(string path, string? link, CopyToOutputDirectoryType copyType)>>(StringComparers.ItemTypes);
                }
                else
                {
                    itemTypes = ItemTypes;
                    itemsByItemTypeBuilder = ItemsByItemType.ToBuilder();
                }

                bool itemsChanged = false;

                foreach ((string itemType, IProjectChangeDescription projectChange) in sourceItemsUpdate.ProjectChanges)
                {
                    if (!itemTypes.Contains(itemType))
                        continue;
                    if (!itemTypesChanged && !projectChange.Difference.AnyChanges)
                        continue;
                    if (projectChange.After.Items.Count == 0)
                        continue;

                    itemsByItemTypeBuilder[itemType] = projectChange.After.Items.Select(item => (item.Key, GetLink(item.Value), GetCopyType(item.Value))).ToImmutableHashSet(UpToDateCheckItemComparer.Instance);
                    itemsChanged = true;
                }

                // NOTE when we previously had zero item types, we can surmise that the project has just been loaded. In such
                // a case it is not correct to assume that the items changed, and so we do not update the timestamp.
                // See https://github.com/dotnet/project-system/issues/5386
                DateTime lastItemsChangedAtUtc = itemsChanged && ItemTypes.Count != 0 ? DateTime.UtcNow : LastItemsChangedAtUtc;

                return new State(
                    msBuildProjectFullPath,
                    msBuildProjectDirectory,
                    markerFile,
                    outputRelativeOrFullPath,
                    newestImportInput,
                    lastVersionSeen: configuredProjectVersion,
                    isDisabled,
                    itemTypes,
                    itemsByItemTypeBuilder.ToImmutable(),
                    customInputs,
                    customOutputs,
                    builtOutputs,
                    copiedOutputFiles,
                    analyzerReferences,
                    compilationReferences,
                    copyReferenceInputs,
                    lastItemsChangedAtUtc,
                    LastCheckedAtUtc);

                static CopyToOutputDirectoryType GetCopyType(IImmutableDictionary<string, string> itemMetadata)
                {
                    if (itemMetadata.TryGetValue(CopyToOutputDirectory, out string value))
                    {
                        if (string.Equals(value, Always, StringComparison.OrdinalIgnoreCase))
                        {
                            return CopyToOutputDirectoryType.CopyAlways;
                        }

                        if (string.Equals(value, PreserveNewest, StringComparison.OrdinalIgnoreCase))
                        {
                            return CopyToOutputDirectoryType.CopyIfNewer;
                        }
                    }

                    return CopyToOutputDirectoryType.CopyNever;
                }

                static string? GetLink(IImmutableDictionary<string, string> itemMetadata)
                {
                    return itemMetadata.TryGetValue(Link, out string link) ? link : null;
                }
            }

            public State WithLastCheckedAtUtc(DateTime lastCheckedAtUtc)
            {
                return new State(
                    MSBuildProjectFullPath,
                    MSBuildProjectDirectory,
                    MarkerFile,
                    OutputRelativeOrFullPath,
                    NewestImportInput,
                    LastVersionSeen,
                    IsDisabled,
                    ItemTypes,
                    ItemsByItemType,
                    CustomInputs,
                    CustomOutputs,
                    BuiltOutputs,
                    CopiedOutputFiles,
                    AnalyzerReferences,
                    CompilationReferences,
                    CopyReferenceInputs,
                    LastItemsChangedAtUtc,
                    lastCheckedAtUtc);
            }

            /// <summary>
            /// For unit tests only.
            /// </summary>
            internal State WithLastItemsChangedAtUtc(DateTime lastItemsChangedAtUtc)
            {
                return new State(
                    MSBuildProjectFullPath,
                    MSBuildProjectDirectory,
                    MarkerFile,
                    OutputRelativeOrFullPath,
                    NewestImportInput,
                    LastVersionSeen,
                    IsDisabled,
                    ItemTypes,
                    ItemsByItemType,
                    CustomInputs,
                    CustomOutputs,
                    BuiltOutputs,
                    CopiedOutputFiles,
                    AnalyzerReferences,
                    CompilationReferences,
                    CopyReferenceInputs,
                    lastItemsChangedAtUtc,
                    LastCheckedAtUtc);
            }
        }
    }
}
