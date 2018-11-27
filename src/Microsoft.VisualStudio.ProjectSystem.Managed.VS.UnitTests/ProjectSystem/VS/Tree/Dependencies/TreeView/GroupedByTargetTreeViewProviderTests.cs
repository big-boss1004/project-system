﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public sealed class GroupedByTargetTreeViewProviderTests
    {
        private readonly ITargetFramework _tfm1 = new TargetFramework("tfm1");
        private readonly ITargetFramework _tfm2 = new TargetFramework("tfm2");

        [Fact]
        public async Task WhenEmptySnapshot_ShouldJustUpdateDependencyRootNode()
        {
            // Arrange
            var treeServices = new MockIDependenciesTreeServices();
            var treeViewModelFactory = IMockDependenciesViewModelFactory.Implement(getDependenciesRootIcon: KnownMonikers.AboutBox);
            var project = UnconfiguredProjectFactory.Create();
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies"
            };
            var targets = new Dictionary<ITargetFramework, ITargetedDependenciesSnapshot>();
            var snapshot = IDependenciesSnapshotFactory.Implement(targets: targets, hasUnresolvedDependency: false);

            // Act
            var provider = new GroupedByTargetTreeViewProvider(treeServices, treeViewModelFactory, commonServices);
            var resultTree = await provider.BuildTreeAsync(dependenciesRoot, snapshot);

            // Assert
            var expectedFlatHierarchy = "Caption=MyDependencies, FilePath=, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=";
            Assert.Equal(expectedFlatHierarchy, ToTestDataString((TestProjectTree)resultTree));
            Assert.Equal(KnownMonikers.AboutBox.ToProjectSystemType(), resultTree.Icon);
            Assert.Equal(KnownMonikers.AboutBox.ToProjectSystemType(), resultTree.ExpandedIcon);
        }

        [Fact]
        public async Task WhenOneTargetSnapshotWithExistingDependencies_ShouldApplyChanges()
        {
            var dependencyModelRootXxx = new TestDependencyModel
            {
                ProviderType = "Xxx",
                Id = "XxxDependencyRoot",
                Name = "XxxDependencyRoot",
                Caption = "XxxDependencyRoot",
                Resolved = true
            };

            var dependencyModelRootYyy = new TestDependencyModel
            {
                ProviderType = "Yyy",
                Id = "YyyDependencyRoot",
                Name = "YyyDependencyRoot",
                Caption = "YyyDependencyRoot"
            };

            var dependencyXxx1 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "tfm1\\xxx\\dependency1",
                Name = "dependency1",
                Path = "dependencyXxxpath",
                Caption = "Dependency1",
                SchemaItemType = "Xxx",
                Resolved = true,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall,
                TargetFramework = _tfm1
            };

            var dependencyYyy1 = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "tfm1\\yyy\\dependency1",
                Name = "dependency1",
                Path = "dependencyYyypath",
                Caption = "Dependency1",
                SchemaItemType = "Yyy",
                Resolved = true,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall,
                TargetFramework = _tfm1
            };

            var dependencyYyyExisting = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "tfm1\\yyy\\dependencyExisting",
                Name = "dependencyExisting",
                Path = "dependencyExistingPath",
                Caption = "DependencyExisting",
                SchemaItemType = "Yyy",
                Resolved = true,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall,
                TargetFramework = _tfm1
            };

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                Children =
                {
                    new TestProjectTree
                    {
                        Caption = "OldRootChildToBeRemoved"
                    },
                    new TestProjectTree
                    {
                        Caption = "YyyDependencyRoot",
                        FilePath = "YyyDependencyRoot",
                        Children =
                        {
                            new TestProjectTree
                            {
                                Caption = "DependencyExisting",
                                FilePath = "tfm1\\yyy\\dependencyExisting"
                            }
                        }
                    }
                }
            };

            var treeViewModelFactory = IMockDependenciesViewModelFactory.Implement(
                getDependenciesRootIcon: KnownMonikers.AboutBox,
                createRootViewModel: new[] { dependencyModelRootXxx, dependencyModelRootYyy });

            var project = UnconfiguredProjectFactory.Create(filePath: @"c:\somefolder\someproject.csproj");
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var provider = new GroupedByTargetTreeViewProvider(
                new MockIDependenciesTreeServices(),
                treeViewModelFactory,
                commonServices);

            var snapshot = GetSnapshot((_tfm1, new[] { dependencyXxx1, dependencyYyy1, dependencyYyyExisting }));

            // Act
            var resultTree = await provider.BuildTreeAsync(dependenciesRoot, snapshot);

            // Assert            
            var expectedFlatHierarchy =
@"Caption=MyDependencies, FilePath=, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=
    Caption=YyyDependencyRoot, FilePath=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
        Caption=DependencyExisting, FilePath=tfm1\yyy\dependencyExisting, IconHash=325249260, ExpandedIconHash=325249260, Rule=, IsProjectItem=False, CustomTag=
        Caption=Dependency1, FilePath=tfm1\Yyy\dependencyYyypath, IconHash=325249260, ExpandedIconHash=325249260, Rule=, IsProjectItem=True, CustomTag=
    Caption=XxxDependencyRoot, FilePath=XxxDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
        Caption=Dependency1, FilePath=tfm1\Xxx\dependencyXxxpath, IconHash=325249260, ExpandedIconHash=325249260, Rule=, IsProjectItem=True, CustomTag=";
            Assert.Equal(expectedFlatHierarchy, ToTestDataString((TestProjectTree)resultTree));
        }

        [Fact]
        public async Task WhenOneTargetSnapshotAndDependencySupportsHierarchyAndIsResolved_ShouldRead()
        {
            var dependencyRootYyy = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "YyyDependencyRoot",
                Name = "YyyDependencyRoot",
                Caption = "YyyDependencyRoot",
                Resolved = true
            };

            var dependencyYyyExisting = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "tfm1\\yyy\\dependencyExisting",
                Name = "dependencyExisting",
                Path = "dependencyExistingpath",
                Caption = "DependencyExisting",
                SchemaItemType = "Yyy",
                Resolved = true,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall,
                Flags = DependencyTreeFlags.SupportsHierarchy,
                TargetFramework = _tfm1
            };

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                Children =
                {
                    new TestProjectTree
                    {
                        Caption = "YyyDependencyRoot",
                        FilePath = "YyyDependencyRoot",
                        Children =
                        {
                            new TestProjectTree
                            {
                                Caption = "DependencyExisting",
                                FilePath = "tfm1\\yyy\\dependencyExisting",
                                CustomTag = "ShouldBeCleanedSinceNodeWillBeRecreated",
                                Flags = DependencyTreeFlags.UnresolvedFlags
                            }
                        }
                    }
                }
            };

            var treeViewModelFactory = IMockDependenciesViewModelFactory.Implement(
                getDependenciesRootIcon: KnownMonikers.AboutBox,
                createRootViewModel: new[] { dependencyRootYyy });

            var project = UnconfiguredProjectFactory.Create(filePath: @"c:\somefolder\someproject.csproj");
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var provider = new GroupedByTargetTreeViewProvider(
                new MockIDependenciesTreeServices(),
                treeViewModelFactory,
                commonServices);

            var snapshot = GetSnapshot((_tfm1, new[] { dependencyYyyExisting }));

            // Act
            var resultTree = await provider.BuildTreeAsync(dependenciesRoot, snapshot);

            // Assert            
            var expectedFlatHierarchy =
@"Caption=MyDependencies, FilePath=, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=
    Caption=YyyDependencyRoot, FilePath=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
        Caption=DependencyExisting, FilePath=tfm1\Yyy\dependencyExistingpath, IconHash=325249260, ExpandedIconHash=325249260, Rule=, IsProjectItem=True, CustomTag=";
            Assert.Equal(expectedFlatHierarchy, ToTestDataString((TestProjectTree)resultTree));
        }

        [Fact]
        public async Task WhenOneTargetSnapshotAndDependencySupportsHierarchyAndIsUnresolved_ShouldRead()
        {
            var dependencyRootYyy = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "YyyDependencyRoot",
                Name = "YyyDependencyRoot",
                Caption = "YyyDependencyRoot",
                Resolved = true
            };

            var dependencyYyyExisting = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "tfm1\\yyy\\dependencyExisting",
                Name = "dependencyExisting",
                Caption = "DependencyExisting",
                SchemaItemType = "Yyy",
                Resolved = false,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall,
                UnresolvedIcon = KnownMonikers.Uninstall,
                UnresolvedExpandedIcon = KnownMonikers.Uninstall,
                Flags = DependencyTreeFlags.SupportsHierarchy
            };

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                Children =
                {
                    new TestProjectTree
                    {
                        Caption = "YyyDependencyRoot",
                        FilePath = "YyyDependencyRoot",
                        Children =
                        {
                            new TestProjectTree
                            {
                                Caption = "DependencyExisting",
                                FilePath = "tfm1\\yyy\\dependencyExisting",
                                CustomTag = "ShouldBeCleanedSinceNodeWillBeRecreated",
                                Flags = DependencyTreeFlags.ResolvedFlags
                            }
                        }
                    }
                }
            };

            var treeViewModelFactory = IMockDependenciesViewModelFactory.Implement(
                getDependenciesRootIcon: KnownMonikers.AboutBox,
                createRootViewModel: new[] { dependencyRootYyy });

            var project = UnconfiguredProjectFactory.Create(filePath: @"c:\somefolder\someproject.csproj");
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var provider = new GroupedByTargetTreeViewProvider(
                new MockIDependenciesTreeServices(),
                treeViewModelFactory,
                commonServices);

            var snapshot = GetSnapshot((_tfm1, new[] { dependencyYyyExisting }));

            // Act
            var resultTree = await provider.BuildTreeAsync(dependenciesRoot, snapshot);

            // Assert            
            var expectedFlatHierarchy =
@"Caption=MyDependencies, FilePath=, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=
    Caption=YyyDependencyRoot, FilePath=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
        Caption=DependencyExisting, FilePath=tfm1\yyy\dependencyExisting, IconHash=325249260, ExpandedIconHash=325249260, Rule=, IsProjectItem=True, CustomTag=";
            Assert.Equal(expectedFlatHierarchy, ToTestDataString((TestProjectTree)resultTree));
        }

        [Fact]
        public async Task WhenOneTargetSnapshotAndDependencySupportsRule_ShouldCreateRule()
        {
            var dependencyRootYyy = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "YyyDependencyRoot",
                Name = "YyyDependencyRoot",
                Caption = "YyyDependencyRoot",
                Resolved = true
            };

            var dependencyYyyExisting = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "tfm1\\yyy\\dependencyExisting",
                Name = "dependencyExisting",
                Caption = "DependencyExisting",
                SchemaItemType = "Yyy",
                Resolved = true,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall,
                Flags = DependencyTreeFlags.SupportsRuleProperties
            };

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                Children =
                {
                    new TestProjectTree
                    {
                        Caption = "YyyDependencyRoot",
                        FilePath = "YyyDependencyRoot",
                        Children =
                        {
                            new TestProjectTree
                            {
                                Caption = "DependencyExisting",
                                FilePath = "tfm1\\yyy\\dependencyExisting",
                                Flags = DependencyTreeFlags.ResolvedFlags
                            }
                        }
                    }
                }
            };

            var treeViewModelFactory = IMockDependenciesViewModelFactory.Implement(
                getDependenciesRootIcon: KnownMonikers.AboutBox,
                createRootViewModel: new[] { dependencyRootYyy });

            var project = UnconfiguredProjectFactory.Create(filePath: @"c:\somefolder\someproject.csproj");
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var provider = new GroupedByTargetTreeViewProvider(
                new MockIDependenciesTreeServices(),
                treeViewModelFactory,
                commonServices);

            var snapshot = GetSnapshot((_tfm1, new[] { dependencyYyyExisting }));

            // Act
            var resultTree = await provider.BuildTreeAsync(dependenciesRoot, snapshot);

            // Assert            
            var expectedFlatHierarchy =
@"Caption=MyDependencies, FilePath=, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=
    Caption=YyyDependencyRoot, FilePath=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
        Caption=DependencyExisting, FilePath=tfm1\yyy\dependencyExisting, IconHash=325249260, ExpandedIconHash=325249260, Rule=Yyy, IsProjectItem=False, CustomTag=";
            Assert.Equal(expectedFlatHierarchy, ToTestDataString((TestProjectTree)resultTree));
        }

        [Fact]
        public async Task WheEmptySnapshotAndVisibilityMarkerProvided_ShouldDisplaySubTreeRoot()
        {
            var dependencyRootYyy = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "YyyDependencyRoot",
                Name = "YyyDependencyRoot",
                Caption = "YyyDependencyRoot",
                Resolved = true
            };

            var dependencyVisibilityMarker = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "someid",
                Name = "someid",
                Caption = "someid",
                Resolved = false,
                Visible = false,
                Flags = DependencyTreeFlags.ShowEmptyProviderRootNode
            };

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                Children =
                {
                    new TestProjectTree
                    {
                        Caption = "YyyDependencyRoot",
                        FilePath = "YyyDependencyRoot"
                    }
                }
            };

            var treeViewModelFactory = IMockDependenciesViewModelFactory.Implement(
                getDependenciesRootIcon: KnownMonikers.AboutBox,
                createRootViewModel: new[] { dependencyRootYyy });

            var project = UnconfiguredProjectFactory.Create(filePath: @"c:\somefolder\someproject.csproj");
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var provider = new GroupedByTargetTreeViewProvider(
                new MockIDependenciesTreeServices(),
                treeViewModelFactory,
                commonServices);

            var snapshot = GetSnapshot((_tfm1, new[] { dependencyVisibilityMarker }));

            // Act
            var resultTree = await provider.BuildTreeAsync(dependenciesRoot, snapshot);

            // Assert            
            var expectedFlatHierarchy =
@"Caption=MyDependencies, FilePath=, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=
    Caption=YyyDependencyRoot, FilePath=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=";
            Assert.Equal(expectedFlatHierarchy, ToTestDataString((TestProjectTree)resultTree));
        }

        [Fact]
        public async Task WheEmptySnapshotAndVisibilityMarkerNotProvided_ShouldHideSubTreeRoot()
        {
            var dependencyModelRootYyy = new TestDependencyModel
            {
                ProviderType = "Yyy",
                Id = "YyyDependencyRoot",
                Name = "YyyDependencyRoot",
                Caption = "YyyDependencyRoot",
                Resolved = true
            };

            var dependencyVisibilityMarker = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "someid",
                Name = "someid",
                Caption = "someid",
                Resolved = false,
                Visible = false
            };

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                Children =
                {
                    new TestProjectTree
                    {
                        Caption = "YyyDependencyRoot",
                        FilePath = "YyyDependencyRoot"
                    }
                }
            };

            var treeViewModelFactory = IMockDependenciesViewModelFactory.Implement(
                getDependenciesRootIcon: KnownMonikers.AboutBox,
                createRootViewModel: new[] { dependencyModelRootYyy });

            var project = UnconfiguredProjectFactory.Create(filePath: @"c:\somefolder\someproject.csproj");
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var provider = new GroupedByTargetTreeViewProvider(
                new MockIDependenciesTreeServices(),
                treeViewModelFactory,
                commonServices);

            var snapshot = GetSnapshot((_tfm1, new[] { dependencyVisibilityMarker }));

            // Act
            var resultTree = await provider.BuildTreeAsync(dependenciesRoot, snapshot);

            // Assert            
            var expectedFlatHierarchy =
@"Caption=MyDependencies, FilePath=, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=";
            Assert.Equal(expectedFlatHierarchy, ToTestDataString((TestProjectTree)resultTree));
        }

        [Fact]
        public async Task WhenMultipleTargetSnapshotsWithExistingDependencies_ShouldApplyChanges()
        {
            var dependencyModelRootXxx = new TestDependencyModel
            {
                ProviderType = "Xxx",
                Id = "XxxDependencyRoot",
                Name = "XxxDependencyRoot",
                Caption = "XxxDependencyRoot",
                Resolved = true
            };

            var dependencyXxx1 = new TestDependency
            {
                ProviderType = "Xxx",
                Id = "xxx\\dependency1",
                Path = "dependencyxxxpath",
                Name = "dependency1",
                Caption = "Dependency1",
                SchemaItemType = "Xxx",
                Resolved = true,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall,
                TargetFramework = _tfm1
            };

            var dependencyModelRootYyy = new TestDependencyModel
            {
                ProviderType = "Yyy",
                Id = "YyyDependencyRoot",
                Name = "YyyDependencyRoot",
                Caption = "YyyDependencyRoot"
            };

            var dependencyYyy1 = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "yyy\\dependency1",
                Path = "dependencyyyypath",
                Name = "dependency1",
                Caption = "Dependency1",
                SchemaItemType = "Yyy",
                Resolved = true,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall,
                TargetFramework = _tfm1
            };

            var dependencyYyyExisting = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "yyy\\dependencyExisting",
                Path = "dependencyyyyExistingpath",
                Name = "dependencyExisting",
                Caption = "DependencyExisting",
                SchemaItemType = "Yyy",
                Resolved = true,
                Icon = KnownMonikers.Uninstall,
                ExpandedIcon = KnownMonikers.Uninstall,
                TargetFramework = _tfm1
            };

            var dependencyModelRootZzz = new TestDependencyModel
            {
                ProviderType = "Zzz",
                Id = "ZzzDependencyRoot",
                Name = "ZzzDependencyRoot",
                Caption = "ZzzDependencyRoot",
                Resolved = true,
                Flags = ProjectTreeFlags.Create(ProjectTreeFlags.Common.BubbleUp)
            };

            var dependencyAny1 = new TestDependency
            {
                ProviderType = "Zzz",
                Id = "ZzzDependencyAny1",
                Path = "ZzzDependencyAny1path",
                Name = "ZzzDependencyAny1",
                Caption = "ZzzDependencyAny1",
                TargetFramework = TargetFramework.Any
            };

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                Children =
                {
                    new TestProjectTree
                    {
                        Caption = "OldRootChildToBeRemoved"
                    },
                    new TestProjectTree
                    {
                        Caption = "YyyDependencyRoot",
                        FilePath = "YyyDependencyRoot",
                        Children =
                        {
                            new TestProjectTree
                            {
                                Caption = "DependencyExisting",
                                FilePath = "yyy\\dependencyExisting"
                            }
                        }
                    }
                }
            };

            var targetModel1 = new TestDependencyModel
            {
                Id = "tfm1",
                Name = "tfm1",
                Caption = "tfm1"
            };

            var targetModel2 = new TestDependencyModel
            {
                Id = "tfm2",
                Name = "tfm2",
                Caption = "tfm2"
            };

            var treeViewModelFactory = IMockDependenciesViewModelFactory.Implement(
                getDependenciesRootIcon: KnownMonikers.AboutBox,
                createRootViewModel: new[] { dependencyModelRootXxx, dependencyModelRootYyy, dependencyModelRootZzz },
                createTargetViewModel: new[] { targetModel1, targetModel2 });

            var project = UnconfiguredProjectFactory.Create(filePath: @"c:\somefolder\someproject.csproj");
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var provider = new GroupedByTargetTreeViewProvider(
                new MockIDependenciesTreeServices(),
                treeViewModelFactory,
                commonServices);

            var snapshot = GetSnapshot(
                (_tfm1, new[] { dependencyXxx1, dependencyYyy1, dependencyYyyExisting }),
                (_tfm2, new[] { dependencyXxx1, dependencyYyy1, dependencyYyyExisting }),
                (TargetFramework.Any, new[] { dependencyAny1 }));

            // Act
            var resultTree = await provider.BuildTreeAsync(dependenciesRoot, snapshot);

            // Assert
            var expectedFlatHierarchy =
@"Caption=MyDependencies, FilePath=, IconHash=325248080, ExpandedIconHash=325248080, Rule=, IsProjectItem=False, CustomTag=
    Caption=ZzzDependencyRoot, FilePath=ZzzDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
        Caption=ZzzDependencyAny1, FilePath=ZzzDependencyAny1, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
    Caption=tfm2, FilePath=tfm2, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=, BubbleUpFlag=True
        Caption=YyyDependencyRoot, FilePath=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
            Caption=DependencyExisting, FilePath=tfm1\Yyy\dependencyyyyExistingpath, IconHash=325249260, ExpandedIconHash=325249260, Rule=, IsProjectItem=False, CustomTag=
            Caption=Dependency1, FilePath=tfm1\Yyy\dependencyyyypath, IconHash=325249260, ExpandedIconHash=325249260, Rule=, IsProjectItem=False, CustomTag=
        Caption=XxxDependencyRoot, FilePath=XxxDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
            Caption=Dependency1, FilePath=tfm1\Xxx\dependencyxxxpath, IconHash=325249260, ExpandedIconHash=325249260, Rule=, IsProjectItem=False, CustomTag=
    Caption=tfm1, FilePath=tfm1, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=, BubbleUpFlag=True
        Caption=YyyDependencyRoot, FilePath=YyyDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
            Caption=DependencyExisting, FilePath=tfm1\Yyy\dependencyyyyExistingpath, IconHash=325249260, ExpandedIconHash=325249260, Rule=, IsProjectItem=True, CustomTag=
            Caption=Dependency1, FilePath=tfm1\Yyy\dependencyyyypath, IconHash=325249260, ExpandedIconHash=325249260, Rule=, IsProjectItem=True, CustomTag=
        Caption=XxxDependencyRoot, FilePath=XxxDependencyRoot, IconHash=0, ExpandedIconHash=0, Rule=, IsProjectItem=False, CustomTag=
            Caption=Dependency1, FilePath=tfm1\Xxx\dependencyxxxpath, IconHash=325249260, ExpandedIconHash=325249260, Rule=, IsProjectItem=True, CustomTag=";
            Assert.Equal(expectedFlatHierarchy, ToTestDataString((TestProjectTree)resultTree));
        }

        [Fact]
        public void WhenFindByPathAndNullNode_ShouldDoNothing()
        {
            // Arrange
            const string projectPath = @"c:\myfolder\mysubfolder\myproject.csproj";
            var projectFolder = Path.GetDirectoryName(projectPath);

            var treeServices = new MockIDependenciesTreeServices();
            var treeViewModelFactory = IMockDependenciesViewModelFactory.Implement();
            var project = UnconfiguredProjectFactory.Create();
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            // Act
            var provider = new GroupedByTargetTreeViewProvider(treeServices, treeViewModelFactory, commonServices);
            var resultTree = provider.FindByPath(null, Path.Combine(projectFolder, @"somenode"));

            // Assert
            Assert.Null(resultTree);
        }

        [Fact]
        public void WhenFindByPathAndNotDependenciesRoot_ShouldDoNothing()
        {
            // Arrange
            const string projectPath = @"c:\myfolder\mysubfolder\myproject.csproj";
            var projectFolder = Path.GetDirectoryName(projectPath);

            var treeServices = new MockIDependenciesTreeServices();
            var treeViewModelFactory = IMockDependenciesViewModelFactory.Implement();
            var project = UnconfiguredProjectFactory.Create();
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies"
            };

            // Act
            var provider = new GroupedByTargetTreeViewProvider(treeServices, treeViewModelFactory, commonServices);
            var resultTree = provider.FindByPath(dependenciesRoot, Path.Combine(projectFolder, @"somenode"));

            // Assert
            Assert.Null(resultTree);
        }

        [Fact]
        public void WhenFindByPathAndAbsoluteNodePath_ShouldFind()
        {
            // Arrange
            var treeServices = new MockIDependenciesTreeServices();
            var treeViewModelFactory = IMockDependenciesViewModelFactory.Implement();
            var project = UnconfiguredProjectFactory.Create();
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                Flags = DependencyTreeFlags.DependenciesRootNodeFlags,
                Children =
                {
                    new TestProjectTree
                    {
                        Caption = "level1Child1",
                        FilePath = @"c:\folder\level1Child1"
                    },
                    new TestProjectTree
                    {
                        Caption = "level1Child2",
                        FilePath = @"c:\folder\level1Child2",
                        Children =
                        {
                            new TestProjectTree
                            {
                                Caption = "level2Child21",
                                FilePath = @"c:\folder\level2Child21"
                            },
                            new TestProjectTree
                            {
                                Caption = "level1Child22",
                                FilePath = @"c:\folder\level2Child22",
                                Children =
                                {
                                    new TestProjectTree
                                    {
                                        Caption = "level3Child31",
                                        FilePath = @"c:\folder\level3Child31"
                                    },
                                    new TestProjectTree
                                    {
                                        Caption = "level3Child32",
                                        FilePath = @"c:\folder\level3Child32"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            // Act
            var provider = new GroupedByTargetTreeViewProvider(treeServices, treeViewModelFactory, commonServices);
            var resultTree = provider.FindByPath(dependenciesRoot, @"c:\folder\level3Child32");

            // Assert
            Assert.NotNull(resultTree);
            Assert.Equal("level3Child32", resultTree.Caption);
        }

        [Fact]
        public void WhenFindByPathAndRelativeNodePath_ShouldNotFind()
        {
            // Arrange
            const string projectPath = @"c:\myfolder\mysubfolder\myproject.csproj";
            var projectFolder = Path.GetDirectoryName(projectPath);

            var treeServices = new MockIDependenciesTreeServices();
            var treeViewModelFactory = IMockDependenciesViewModelFactory.Implement();
            var project = UnconfiguredProjectFactory.Create(filePath: projectPath);
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var dependenciesRoot = new TestProjectTree
            {
                Caption = "MyDependencies",
                Flags = DependencyTreeFlags.DependenciesRootNodeFlags,
                Children =
                {
                    new TestProjectTree
                    {
                        Caption = "level1Child1",
                        FilePath = @"c:\folder\level1Child1"
                    },
                    new TestProjectTree
                    {
                        Caption = "level1Child2",
                        FilePath = @"c:\folder\level1Child2",
                        Children =
                        {
                            new TestProjectTree
                            {
                                Caption = "level2Child21",
                                FilePath = @"c:\folder\level2Child21"
                            },
                            new TestProjectTree
                            {
                                Caption = "level1Child22",
                                FilePath = @"c:\folder\level2Child22",
                                Children =
                                {
                                    new TestProjectTree
                                    {
                                        Caption = "level3Child31",
                                        FilePath = @"c:\folder\level3Child31"
                                    },
                                    new TestProjectTree
                                    {
                                        Caption = "level3Child32",
                                        FilePath = @"level3Child32"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            // Act
            var provider = new GroupedByTargetTreeViewProvider(treeServices, treeViewModelFactory, commonServices);
            var resultTree = provider.FindByPath(dependenciesRoot, Path.Combine(projectFolder, @"level3Child32"));

            // Assert
            Assert.Null(resultTree);
        }

        [Fact]
        public void WhenFindByPathAndNeedToFindDependenciesRoot_ShouldNotFind()
        {
            // Arrange
            const string projectPath = @"c:\myfolder\mysubfolder\myproject.csproj";
            var projectFolder = Path.GetDirectoryName(projectPath);

            var treeServices = new MockIDependenciesTreeServices();
            var treeViewModelFactory = IMockDependenciesViewModelFactory.Implement();
            var project = UnconfiguredProjectFactory.Create(filePath: projectPath);
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var projectRoot = new TestProjectTree
            {
                Caption = "myproject",
                Children =
                {
                    new TestProjectTree
                    {
                        Caption = "MyDependencies",
                        Flags = DependencyTreeFlags.DependenciesRootNodeFlags,
                        Children =
                        {
                            new TestProjectTree
                            {
                                Caption = "level1Child1",
                                FilePath = @"c:\folder\level1Child1"
                            },
                            new TestProjectTree
                            {
                                Caption = "level1Child2",
                                FilePath = @"c:\folder\level1Child2",
                                Children =
                                {
                                    new TestProjectTree
                                    {
                                        Caption = "level2Child21",
                                        FilePath = @"c:\folder\level2Child21"
                                    },
                                    new TestProjectTree
                                    {
                                        Caption = "level1Child22",
                                        FilePath = @"c:\folder\level2Child22",
                                        Children =
                                        {
                                            new TestProjectTree
                                            {
                                                Caption = "level3Child31",
                                                FilePath = @"c:\folder\level3Child31"
                                            },
                                            new TestProjectTree
                                            {
                                                Caption = "level3Child32",
                                                FilePath = @"level3Child32"
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            // Act
            var provider = new GroupedByTargetTreeViewProvider(treeServices, treeViewModelFactory, commonServices);

            var result = provider.FindByPath(projectRoot, Path.Combine(projectFolder, @"level3Child32"));

            // Assert
            Assert.Null(result);
        }

        private static IDependenciesSnapshot GetSnapshot(params (ITargetFramework tfm, IReadOnlyList<IDependency> dependencies)[] testData)
        {
            var catalogs = IProjectCatalogSnapshotFactory.Create();
            var targets = new Dictionary<ITargetFramework, ITargetedDependenciesSnapshot>();

            foreach ((ITargetFramework tfm, IReadOnlyList<IDependency> dependencies) in testData)
            {
                var targetedSnapshot = ITargetedDependenciesSnapshotFactory.Implement(
                    catalogs: catalogs,
                    topLevelDependencies: dependencies,
                    checkForUnresolvedDependencies: false,
                    targetFramework: tfm);

                targets.Add(tfm, targetedSnapshot);
            }

            return IDependenciesSnapshotFactory.Implement(
                targets: targets,
                hasUnresolvedDependency: false,
                activeTarget: testData[0].tfm);
        }

        private static string ToTestDataString(TestProjectTree root)
        {
            var builder = new StringBuilder();

            GetChildrenTestStats(root, indent: 0);

            return builder.ToString();

            void GetChildrenTestStats(TestProjectTree tree, int indent)
            {
                WriteLine();

                foreach (var child in tree.Children)
                {
                    builder.AppendLine();
                    GetChildrenTestStats(child, indent + 1);
                }

                void WriteLine()
                {
                    builder.Append(' ', indent * 4);
                    builder.Append("Caption=").Append(tree.Caption).Append(", ");
                    builder.Append("FilePath=").Append(tree.FilePath).Append(", ");
                    builder.Append("IconHash=").Append(tree.Icon.GetHashCode()).Append(", ");
                    builder.Append("ExpandedIconHash=").Append(tree.ExpandedIcon.GetHashCode()).Append(", ");
                    builder.Append("Rule=").Append(tree.BrowseObjectProperties?.Name ?? "").Append(", ");
                    builder.Append("IsProjectItem=").Append(tree.IsProjectItem).Append(", ");
                    builder.Append("CustomTag=").Append(tree.CustomTag);

                    if (tree.Flags.Contains(ProjectTreeFlags.Common.BubbleUp))
                    {
                        builder.Append(", BubbleUpFlag=True");
                    }
                }
            }
        }
    }
}
