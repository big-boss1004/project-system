﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    public sealed class TargetedDependenciesSnapshotTests
    {
        [Fact]
        public void Constructor_WhenRequiredParamsNotProvided_ShouldThrow()
        {
            var path = "path";
            var tfm = TargetFramework.Any;
            var deps = ImmutableArray<IDependency>.Empty;

            Assert.Throws<ArgumentNullException>("projectPath",       () => new TargetedDependenciesSnapshot(null!, tfm,   null, deps));
            Assert.Throws<ArgumentNullException>("targetFramework",   () => new TargetedDependenciesSnapshot(path,  null!, null, deps));
        }

        [Fact]
        public void Constructor()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = new TargetFramework("tfm1");

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var snapshot = new TargetedDependenciesSnapshot(
                projectPath,
                targetFramework,
                catalogs,
                ImmutableArray<IDependency>.Empty);

            Assert.Same(projectPath, snapshot.ProjectPath);
            Assert.Same(targetFramework, snapshot.TargetFramework);
            Assert.Same(catalogs, snapshot.Catalogs);
            Assert.False(snapshot.HasVisibleUnresolvedDependency);
            Assert.Empty(snapshot.Dependencies);
            Assert.False(snapshot.CheckForUnresolvedDependencies("foo"));
        }

        [Fact]
        public void CreateEmpty()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = new TargetFramework("tfm1");
            var catalogs = IProjectCatalogSnapshotFactory.Create();

            var snapshot = TargetedDependenciesSnapshot.CreateEmpty(projectPath, targetFramework, catalogs);

            Assert.Same(projectPath, snapshot.ProjectPath);
            Assert.Same(targetFramework, snapshot.TargetFramework);
            Assert.Same(catalogs, snapshot.Catalogs);
            Assert.False(snapshot.HasVisibleUnresolvedDependency);
            Assert.Empty(snapshot.Dependencies);
            Assert.False(snapshot.CheckForUnresolvedDependencies("foo"));
        }

        [Fact]
        public void FromChanges_NoChanges()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = new TargetFramework("tfm1");
            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var previousSnapshot = TargetedDependenciesSnapshot.CreateEmpty(projectPath, targetFramework, catalogs);

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                previousSnapshot,
                changes: null,
                catalogs,
                ImmutableArray<IDependenciesSnapshotFilter>.Empty,
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.Same(previousSnapshot, snapshot);
        }

        [Fact]
        public void FromChanges_CatalogChanged()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = new TargetFramework("tfm1");
            var previousCatalogs = IProjectCatalogSnapshotFactory.Create();
            var previousSnapshot = TargetedDependenciesSnapshot.CreateEmpty(projectPath, targetFramework, previousCatalogs);

            var updatedCatalogs = IProjectCatalogSnapshotFactory.Create();

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                previousSnapshot,
                changes: null,
                updatedCatalogs,
                ImmutableArray<IDependenciesSnapshotFilter>.Empty,
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.NotSame(previousSnapshot, snapshot);
            Assert.Same(projectPath, snapshot.ProjectPath);
            Assert.Same(updatedCatalogs, snapshot.Catalogs);
            Assert.Equal(previousSnapshot.Dependencies.Length, snapshot.Dependencies.Length);
            for (int i = 0; i < previousSnapshot.Dependencies.Length; i++)
            {
                Assert.Same(previousSnapshot.Dependencies[i], snapshot.Dependencies[i]);
            }
            Assert.False(snapshot.HasVisibleUnresolvedDependency);
            Assert.Empty(snapshot.Dependencies);
        }

        [Fact]
        public void FromChanges_AddingToEmpty()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = new TargetFramework("tfm1");

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var previousSnapshot = TargetedDependenciesSnapshot.CreateEmpty(projectPath, targetFramework, catalogs);

            var resolved = new TestDependencyModel
            {
                ProviderType = "Xxx",
                Id = "dependency1",
                Name = "Dependency1",
                Caption = "Dependency1",
                Resolved = true,
                Flags = DependencyTreeFlags.Resolved,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall
            };

            var unresolved = new TestDependencyModel
            {
                ProviderType = "Xxx",
                Id = "dependency2",
                Name = "Dependency2",
                Caption = "Dependency2",
                Resolved = false,
                Flags = DependencyTreeFlags.Unresolved,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall
            };

            var changes = new DependenciesChangesBuilder();
            changes.Added(resolved);
            changes.Added(unresolved);

            const string updatedProjectPath = "updatedProjectPath";

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                updatedProjectPath,
                previousSnapshot,
                changes.TryBuildChanges()!,
                catalogs,
                ImmutableArray<IDependenciesSnapshotFilter>.Empty,
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.NotSame(previousSnapshot, snapshot);
            Assert.Same(updatedProjectPath, snapshot.ProjectPath);
            Assert.Same(catalogs, snapshot.Catalogs);
            Assert.True(snapshot.HasVisibleUnresolvedDependency);
            AssertEx.CollectionLength(snapshot.Dependencies, 2);
            Assert.Contains(snapshot.Dependencies, dep => resolved.Matches(dep, targetFramework));
            Assert.Contains(snapshot.Dependencies, dep => unresolved.Matches(dep, targetFramework));
        }

        [Fact]
        public void FromChanges_NoChangesAfterBeforeRemoveFilterDeclinedChange()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";

            var targetFramework = new TargetFramework("tfm1");

            var dependency1 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = Dependency.GetID(targetFramework, "Xxx", "dependency1"),
                Name = "Dependency1",
                Caption = "Dependency1",
                SchemaItemType = "Xxx",
                Resolved = true
            };

            var dependency2 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = Dependency.GetID(targetFramework, "Xxx", "dependency2"),
                Name = "Dependency1",
                Caption = "Dependency1",
                SchemaItemType = "Xxx",
                Resolved = true
            };

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var previousSnapshot = new TargetedDependenciesSnapshot(
                projectPath,
                targetFramework,
                catalogs,
                ImmutableArray.Create<IDependency>(dependency1, dependency2));

            var changes = new DependenciesChangesBuilder();
            changes.Removed(dependency1.ProviderType, dependency1.Id);

            var snapshotFilter = new TestDependenciesSnapshotFilter();

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                previousSnapshot,
                changes.TryBuildChanges()!,
                catalogs,
                ImmutableArray.Create<IDependenciesSnapshotFilter>(snapshotFilter),
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.Same(previousSnapshot, snapshot);
            Assert.True(snapshotFilter.Completed);
        }

        [Fact]
        public void FromChanges_ReportedChangesAfterBeforeRemoveFilterDeclinedChange()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = new TargetFramework("tfm1");

            var dependency1 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "tfm1\\xxx\\dependency1",
                Name = "Dependency1",
                Caption = "Dependency1",
                SchemaItemType = "Xxx",
                Resolved = true
            };

            var dependency2 = new TestDependency
            {
                ProviderType =  "Xxx",
                Id = "tfm1\\xxx\\dependency2",
                Name = "Dependency2",
                Caption = "Dependency2",
                SchemaItemType = "Xxx",
                Resolved = true
            };

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var previousSnapshot = new TargetedDependenciesSnapshot(
                projectPath,
                targetFramework,
                catalogs,
                ImmutableArray.Create<IDependency>(dependency1, dependency2));

            var changes = new DependenciesChangesBuilder();
            changes.Removed("Xxx", "dependency1");

            var addedOnRemove = new TestDependency { Id = "SomethingElse" };

            var snapshotFilter = new TestDependenciesSnapshotFilter()
                .BeforeRemoveReject(@"tfm1\xxx\dependency1", addOrUpdate: addedOnRemove);

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                previousSnapshot,
                changes.TryBuildChanges()!,
                catalogs,
                ImmutableArray.Create<IDependenciesSnapshotFilter>(snapshotFilter),
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.True(snapshotFilter.Completed);

            Assert.NotSame(previousSnapshot, snapshot);

            Assert.Same(previousSnapshot.TargetFramework, snapshot.TargetFramework);
            Assert.Same(projectPath, snapshot.ProjectPath);
            Assert.Same(catalogs, snapshot.Catalogs);
            AssertEx.CollectionLength(snapshot.Dependencies, 3);
            Assert.Contains(addedOnRemove, snapshot.Dependencies);
        }

        [Fact]
        public void FromChanges_NoChangesAfterBeforeAddFilterDeclinedChange()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = new TargetFramework("tfm1");

            var dependency1 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = Dependency.GetID(targetFramework, "Xxx", "dependency1"),
                Name = "Dependency1",
                Caption = "Dependency1",
                SchemaItemType = "Xxx",
                Resolved = true
            };

            var dependencyModelNew1 = new TestDependencyModel
            {
                ProviderType =  "Xxx",
                Id =  "newdependency1",
                Name = "NewDependency1",
                Caption = "NewDependency1",
                SchemaItemType = "Xxx",
                Resolved = true,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall
            };

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var previousSnapshot = new TargetedDependenciesSnapshot(
                projectPath,
                targetFramework,
                catalogs,
                ImmutableArray.Create<IDependency>(dependency1));

            var changes = new DependenciesChangesBuilder();
            changes.Added(dependencyModelNew1);

            var snapshotFilter = new TestDependenciesSnapshotFilter()
                .BeforeAddReject(@"tfm1\xxx\newdependency1");

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                previousSnapshot,
                changes.TryBuildChanges()!,
                catalogs,
                ImmutableArray.Create<IDependenciesSnapshotFilter>(snapshotFilter),
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.True(snapshotFilter.Completed);

            Assert.Same(previousSnapshot, snapshot);
        }

        [Fact]
        public void FromChanges_ReportedChangesAfterBeforeAddFilterDeclinedChange()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = new TargetFramework("tfm1");

            var dependency1 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "tfm1\\xxx\\dependency1",
                Name = "Dependency1",
                Caption = "Dependency1",
                SchemaItemType = "Xxx",
                Resolved = true
            };

            var dependency2 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "tfm1\\xxx\\dependency2",
                Name = "Dependency2",
                Caption = "Dependency2",
                SchemaItemType = "Xxx",
                Resolved = true
            };

            var dependencyModelNew1 = new TestDependencyModel
            {
                ProviderType = "Xxx",
                Id = "newdependency1",
                Name = "NewDependency1",
                Caption = "NewDependency1",
                SchemaItemType = "Xxx",
                Resolved = true,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall
            };

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var previousSnapshot = new TargetedDependenciesSnapshot(
                projectPath,
                targetFramework,
                catalogs,
                ImmutableArray.Create<IDependency>(dependency1, dependency2));

            var changes = new DependenciesChangesBuilder();
            changes.Added(dependencyModelNew1);

            var filterAddedDependency = new TestDependency { Id = "unexpected" };

            var snapshotFilter = new TestDependenciesSnapshotFilter()
                .BeforeAddReject(@"tfm1\xxx\newdependency1", addOrUpdate: filterAddedDependency);

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                previousSnapshot,
                changes.TryBuildChanges()!,
                catalogs,
                ImmutableArray.Create<IDependenciesSnapshotFilter>(snapshotFilter),
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.True(snapshotFilter.Completed);

            Assert.NotSame(previousSnapshot, snapshot);

            Assert.Same(previousSnapshot.TargetFramework, snapshot.TargetFramework);
            Assert.Same(previousSnapshot.ProjectPath, snapshot.ProjectPath);
            Assert.Same(previousSnapshot.Catalogs, snapshot.Catalogs);

            AssertEx.CollectionLength(snapshot.Dependencies, 3);
            Assert.Contains(dependency1, snapshot.Dependencies);
            Assert.Contains(dependency2, snapshot.Dependencies);
            Assert.Contains(filterAddedDependency, snapshot.Dependencies);
        }

        [Fact]
        public void FromChanges_RemovedAndAddedChanges()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = new TargetFramework("tfm1");

            var dependency1 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "tfm1\\xxx\\dependency1",
                Name = "Dependency1",
                Caption = "Dependency1",
                SchemaItemType = "Xxx",
                Resolved = true
            };

            var dependency2 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "tfm1\\xxx\\dependency2",
                Name = "Dependency2",
                Caption = "Dependency2",
                SchemaItemType = "Xxx",
                Resolved = true
            };

            var dependencyModelAdded1 = new TestDependencyModel
            {
                ProviderType = "Xxx",
                Id = "addeddependency1",
                Name = "AddedDependency1",
                Caption = "AddedDependency1",
                SchemaItemType = "Xxx",
                Resolved = true,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall
            };

            var dependencyModelAdded2 = new TestDependencyModel
            {
                ProviderType = "Xxx",
                Id = "addeddependency2",
                Name = "AddedDependency2",
                Caption = "AddedDependency2",
                SchemaItemType = "Xxx",
                Resolved = true,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall
            };

            var dependencyModelAdded3 = new TestDependencyModel
            {
                ProviderType = "Xxx",
                Id = "addeddependency3",
                Name = "AddedDependency3",
                Caption = "AddedDependency3",
                SchemaItemType = "Xxx",
                Resolved = true,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall
            };

            var dependencyAdded2Changed = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "tfm1\\xxx\\addeddependency2",
                Name = "AddedDependency2Changed",
                Caption = "AddedDependency2Changed",
                SchemaItemType = "Xxx",
                Resolved = true
            };

            var dependencyRemoved1 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "tfm1\\xxx\\Removeddependency1",
                Name = "RemovedDependency1",
                Caption = "RemovedDependency1",
                SchemaItemType = "Xxx",
                Resolved = true
            };

            var dependencyInsteadRemoved1 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "tfm1\\xxx\\InsteadRemoveddependency1",
                Name = "InsteadRemovedDependency1",
                Caption = "InsteadRemovedDependency1",
                SchemaItemType = "Xxx",
                Resolved = true
            };

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var previousSnapshot = new TargetedDependenciesSnapshot(
                projectPath,
                targetFramework,
                catalogs,
                ImmutableArray.Create<IDependency>(dependency1, dependency2, dependencyRemoved1));

            var changes = new DependenciesChangesBuilder();
            changes.Added(dependencyModelAdded1);
            changes.Added(dependencyModelAdded2);
            changes.Added(dependencyModelAdded3);
            changes.Removed("Xxx", "Removeddependency1");

            var snapshotFilter = new TestDependenciesSnapshotFilter()
                .BeforeAddReject(@"tfm1\xxx\addeddependency1")
                .BeforeAddAccept(@"tfm1\xxx\addeddependency2", dependencyAdded2Changed)
                .BeforeAddAccept(@"tfm1\xxx\addeddependency3")
                .BeforeRemoveAccept(@"tfm1\xxx\Removeddependency1", dependencyInsteadRemoved1);

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                previousSnapshot,
                changes.TryBuildChanges()!,
                catalogs,
                ImmutableArray.Create<IDependenciesSnapshotFilter>(snapshotFilter),
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.True(snapshotFilter.Completed);

            Assert.NotSame(previousSnapshot, snapshot);

            Assert.Same(previousSnapshot.TargetFramework, snapshot.TargetFramework);
            Assert.Same(projectPath, snapshot.ProjectPath);
            Assert.Same(catalogs, snapshot.Catalogs);
            AssertEx.CollectionLength(snapshot.Dependencies, 5);
            Assert.Contains(snapshot.Dependencies, dep => dep.Id == @"tfm1\xxx\dependency1");
            Assert.Contains(snapshot.Dependencies, dep => dep.Id == @"tfm1\xxx\dependency2");
            Assert.Contains(snapshot.Dependencies, dep => dep.Id == @"tfm1\xxx\addeddependency2");
            Assert.Contains(snapshot.Dependencies, dep => dep.Id == @"tfm1\xxx\InsteadRemoveddependency1");
            Assert.Contains(snapshot.Dependencies, dep => dep.Id == @"tfm1\Xxx\addeddependency3");
        }

        [Fact]
        public void FromChanges_UpdatesLevelDependencies()
        {
            const string projectPath = @"c:\somefolder\someproject\a.csproj";
            var targetFramework = new TargetFramework("tfm1");

            var dependencyPrevious = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "tfm1\\xxx\\dependency1",
                Name = "Dependency1",
                Caption = "Dependency1",
                SchemaItemType = "Xxx",
                Resolved = true
            };

            var dependencyModelAdded = new TestDependencyModel
            {
                ProviderType = "Xxx",
                Id = "dependency1",
                Name = "Dependency1",
                Caption = "Dependency1",
                SchemaItemType = "Xxx",
                Resolved = true,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall
            };

            var dependencyUpdated = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "tfm1\\xxx\\dependency1",
                Name = "Dependency1",
                Caption = "Dependency1",
                SchemaItemType = "Xxx",
                Resolved = true
            };

            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var previousSnapshot = new TargetedDependenciesSnapshot(
                projectPath,
                targetFramework,
                catalogs,
                ImmutableArray.Create<IDependency>(dependencyPrevious));

            var changes = new DependenciesChangesBuilder();
            changes.Added(dependencyModelAdded);

            var snapshotFilter = new TestDependenciesSnapshotFilter()
                    .BeforeAddAccept(@"tfm1\xxx\dependency1", dependencyUpdated);

            var snapshot = TargetedDependenciesSnapshot.FromChanges(
                projectPath,
                previousSnapshot,
                changes.TryBuildChanges()!,
                catalogs,
                ImmutableArray.Create<IDependenciesSnapshotFilter>(snapshotFilter),
                new Dictionary<string, IProjectDependenciesSubTreeProvider>(),
                null);

            Assert.True(snapshotFilter.Completed);

            Assert.NotSame(previousSnapshot, snapshot);
            Assert.Same(dependencyUpdated, snapshot.Dependencies.Single());
        }

        internal sealed class TestDependenciesSnapshotFilter : IDependenciesSnapshotFilter
        {
            private enum FilterAction { Reject, Accept }

            private readonly Dictionary<string, (FilterAction, IDependency?)> _beforeAdd    = new Dictionary<string, (FilterAction, IDependency?)>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, (FilterAction, IDependency?)> _beforeRemove = new Dictionary<string, (FilterAction, IDependency?)>(StringComparer.OrdinalIgnoreCase);

            public TestDependenciesSnapshotFilter BeforeAddAccept(string id, IDependency? dependency = null)
            {
                _beforeAdd.Add(id, (FilterAction.Accept, dependency));
                return this;
            }

            public TestDependenciesSnapshotFilter BeforeAddReject(string id, IDependency? addOrUpdate = null)
            {
                _beforeAdd.Add(id, (FilterAction.Reject, addOrUpdate));
                return this;
            }

            public TestDependenciesSnapshotFilter BeforeRemoveAccept(string id, IDependency? addOrUpdate = null)
            {
                _beforeRemove.Add(id, (FilterAction.Accept, addOrUpdate));
                return this;
            }

            public TestDependenciesSnapshotFilter BeforeRemoveReject(string id, IDependency? addOrUpdate = null)
            {
                _beforeRemove.Add(id, (FilterAction.Reject, addOrUpdate));
                return this;
            }

            public void BeforeAddOrUpdate(
                ITargetFramework targetFramework,
                IDependency dependency,
                IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviderByProviderType,
                IImmutableSet<string>? projectItemSpecs,
                AddDependencyContext context)
            {
                if (_beforeAdd.TryGetValue(dependency.Id, out (FilterAction Action, IDependency? Dependency) info))
                {
                    if (info.Action == FilterAction.Reject)
                    {
                        context.Reject();

                        if (info.Dependency != null)
                        {
                            context.AddOrUpdate(info.Dependency);
                        }
                    }
                    else if (info.Action == FilterAction.Accept)
                    {
                        context.Accept(info.Dependency ?? dependency);
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }

                    _beforeAdd.Remove(dependency.Id);
                }
                else
                {
                    throw new ArgumentException("Unexpected dependency ID: " + dependency.Id);
                }
            }

            public void BeforeRemove(
                ITargetFramework targetFramework,
                IDependency dependency,
                RemoveDependencyContext context)
            {
                if (_beforeRemove.TryGetValue(dependency.Id, out (FilterAction Action, IDependency? Dependency) info))
                {
                    if (info.Action == FilterAction.Reject)
                    {
                        context.Reject();

                        if (info.Dependency != null)
                        {
                            context.AddOrUpdate(info.Dependency);
                        }
                    }
                    else if (info.Action == FilterAction.Accept)
                    {
                        context.Accept();

                        if (info.Dependency != null)
                        {
                            context.AddOrUpdate(info.Dependency);
                        }
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }

                    _beforeRemove.Remove(dependency.Id);
                }
                else
                {
                    throw new ArgumentException("Unexpected dependency ID: " + dependency.Id);
                }
            }

            public bool Completed => _beforeAdd.Count == 0 && _beforeRemove.Count == 0;
        }
    }
}
