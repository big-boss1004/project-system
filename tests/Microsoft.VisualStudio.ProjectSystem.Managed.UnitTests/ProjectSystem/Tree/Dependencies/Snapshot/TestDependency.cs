﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot
{
    [DebuggerDisplay("{" + nameof(Id) + ",nq}")]
    internal sealed class TestDependency : IDependency
    {
        private static readonly DependencyIconSet s_defaultIconSet = new DependencyIconSet(
            KnownMonikers.Accordian,
            KnownMonikers.Bug,
            KnownMonikers.CrashDumpFile,
            KnownMonikers.DataCenter);

        public IDependency ClonePropertiesFrom
        {
            set
            {
                ProviderType = value.ProviderType;
                Caption = value.Caption;
                OriginalItemSpec = value.OriginalItemSpec;
                FilePath = value.FilePath;
                SchemaName = value.SchemaName;
                SchemaItemType = value.SchemaItemType;
                Resolved = value.Resolved;
                Implicit = value.Implicit;
                Visible = value.Visible;
                IconSet = value.IconSet;
                BrowseObjectProperties = value.BrowseObjectProperties;
                Flags = value.Flags;
                Id = value.Id;
            }
        }

#pragma warning disable CS8618 // Non-nullable property is uninitialized
        public string ProviderType { get; set; } = "provider";
        public string Caption { get; set; }
        public string? OriginalItemSpec { get; set; }
        public string? SchemaName { get; set; }
        public string? SchemaItemType { get; set; }
        public DiagnosticLevel DiagnosticLevel { get; set; } = DiagnosticLevel.None;
        public bool Resolved { get; set; } = false;
        public bool Implicit { get; set; } = false;
        public bool Visible { get; set; } = true;
        public IImmutableDictionary<string, string> BrowseObjectProperties { get; set; }
        public ProjectTreeFlags Flags { get; set; } = ProjectTreeFlags.Empty;
        public string Id { get; set; }
        public DependencyIconSet IconSet { get; set; } = s_defaultIconSet;
        public string? FilePath { get; set; }
        public ImageMoniker Icon => Resolved ? IconSet.Icon : IconSet.UnresolvedIcon;
        public ImageMoniker ExpandedIcon => Resolved ? IconSet.ExpandedIcon : IconSet.UnresolvedExpandedIcon;
#pragma warning restore CS8618 // Non-nullable property is uninitialized

        public IDependency SetProperties(
            string? caption = null,
            bool? resolved = null,
            ProjectTreeFlags? flags = null,
            string? schemaName = null,
            DependencyIconSet? iconSet = null,
            bool? isImplicit = null)
        {
            return new TestDependency
            {
                // Copy all properties from this instance
                ClonePropertiesFrom = this,

                // Override specific properties as needed
                Caption = caption ?? Caption,
                Resolved = resolved ?? Resolved,
                Flags = flags ?? Flags,
                SchemaName = schemaName ?? SchemaName,
                IconSet = iconSet ?? IconSet,
                Implicit = isImplicit ?? Implicit
            };
        }
    }
}
