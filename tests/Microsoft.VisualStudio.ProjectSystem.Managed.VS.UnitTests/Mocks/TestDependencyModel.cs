﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal class TestDependencyModel : IDependencyModel
    {
#pragma warning disable CS8618 // Non-nullable property is uninitialized
        public string ProviderType { get; set; }
        public string Name { get; set; }
        public string Caption { get; set; }
        public string OriginalItemSpec { get; set; }
        public string Path { get; set; }
        public string SchemaName { get; set; }
        public string? SchemaItemType { get; set; }
        public string Version => throw new NotImplementedException();
        public bool Resolved { get; set; } = false;
        public bool TopLevel => true;
        public bool Implicit { get; set; } = false;
        public bool Visible { get; set; } = true;
        public int Priority => throw new NotImplementedException();
        public ImageMoniker Icon { get; set; }
        public ImageMoniker ExpandedIcon { get; set; }
        public ImageMoniker UnresolvedIcon { get; set; }
        public ImageMoniker UnresolvedExpandedIcon { get; set; }
        public IImmutableDictionary<string, string> Properties { get; set; }
        public IImmutableList<string> DependencyIDs => throw new NotImplementedException();
        public ProjectTreeFlags Flags { get; set; } = ProjectTreeFlags.Empty;
        public string Id { get; set; }
#pragma warning restore CS8618 // Non-nullable property is uninitialized

        public bool Matches(IDependency dependency, ITargetFramework tfm)
        {
            return Dependency.GetID(tfm, ProviderType, Id) == dependency.Id
                   && ProviderType == dependency.ProviderType
                   && Flags == dependency.Flags
                   && (Name == null || Name == dependency.Name)
                   && (Caption == null || Caption == dependency.Caption)
                   && (OriginalItemSpec == null || OriginalItemSpec == dependency.OriginalItemSpec)
                   && (Path == null || OriginalItemSpec == dependency.Path)
                   && (SchemaName == null || SchemaName == dependency.SchemaName)
                   && (SchemaItemType == null || !Flags.Contains(DependencyTreeFlags.GenericDependency) || SchemaItemType == dependency.SchemaItemType)
                   && Resolved == dependency.Resolved
                   && Implicit == dependency.Implicit
                   && Visible == dependency.Visible
                   && Equals(Icon, dependency.IconSet.Icon)
                   && Equals(ExpandedIcon, dependency.IconSet.ExpandedIcon)
                   && Equals(UnresolvedIcon, dependency.IconSet.UnresolvedIcon)
                   && Equals(UnresolvedExpandedIcon, dependency.IconSet.UnresolvedExpandedIcon)
                   && Equals(Properties, dependency.BrowseObjectProperties);
        }

        private static bool Equals(IImmutableDictionary<string, string> a, IImmutableDictionary<string, string> b)
        {
            // Allow b to have whatever if we didn't specify any properties
            if (a == null || a.Count == 0)
                return true;

            return a.Count == b.Count &&
                   a.All(pair => b.TryGetValue(pair.Key, out var value) && value == pair.Value);
        }

        private static bool Equals(ImageMoniker a, ImageMoniker b) => a.Id == b.Id && a.Guid == b.Guid;
    }
}
