﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters
{
    /// <summary>
    /// Context used by <see cref="IDependenciesSnapshotFilter"/> implementations when filtering
    /// a dependency that is being removed from a <see cref="DependenciesSnapshot"/> by
    /// <see cref="DependenciesSnapshot.FromChanges"/>.
    /// </summary>
    internal sealed class RemoveDependencyContext
    {
        private readonly ImmutableDictionary<string, IDependency>.Builder _worldBuilder;

        private bool? _acceptedOrRejected;
        private IDependency? _acceptedDependency;

        public bool Changed { get; private set; }

        public RemoveDependencyContext(ImmutableDictionary<string, IDependency>.Builder worldBuilder)
        {
            _worldBuilder = worldBuilder;
        }

        public void Reset()
        {
            Changed = false;
            _acceptedOrRejected = null;
            _acceptedDependency = null;
        }

        public bool GetResult(IDependenciesSnapshotFilter filter)
        {
            if (_acceptedOrRejected == null)
            {
                throw new InvalidOperationException(
                    $"Filter type {filter.GetType()} must either accept or reject the dependency.");
            }

            bool? value = _acceptedOrRejected;
            _acceptedOrRejected = null;
            return value.Value;
        }

        /// <summary>
        /// Attempts to find the dependency in the project's tree with specified <paramref name="dependencyId"/>.
        /// </summary>
        public bool TryGetDependency(string dependencyId, out IDependency dependency)
        {
            return _worldBuilder.TryGetValue(dependencyId, out dependency);
        }

        /// <summary>
        /// Adds a new, or replaces an existing dependency (keyed on <see cref="IDependency.Id"/>).
        /// </summary>
        /// <remarks>
        /// In the course of filtering one dependency, the filter may wish to modify or add other
        /// dependencies in the project's tree. This method allows that to happen.
        /// </remarks>
        public void AddOrUpdate(IDependency dependency)
        {
            _worldBuilder.Remove(dependency.Id);
            _worldBuilder.Add(dependency.Id, dependency);
            Changed = true;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the project tree contains a dependency with specified <paramref name="dependencyId"/>.
        /// </summary>
        public bool Contains(string dependencyId)
        {
            return _worldBuilder.ContainsKey(dependencyId);
        }

        /// <summary>
        /// Returns an enumerator over all dependencies in the project tree.
        /// </summary>
        /// <returns></returns>
        public ImmutableDictionary<string, IDependency>.Enumerator GetEnumerator()
        {
            return _worldBuilder.GetEnumerator();
        }

        /// <summary>
        /// Indicates the filter allows the removal of the dependency from the snapshot.
        /// </summary>
        public void Accept()
        {
            if (_acceptedOrRejected != null)
            {
                throw new InvalidOperationException(
                    $"Filter has already {(_acceptedDependency == null ? "rejected the" : "accepted a")} dependency.");
            }

            _acceptedOrRejected = true;
        }

        /// <summary>
        /// Indicates the filter rejects the removal of the dependency from the snapshot.
        /// </summary>
        public void Reject()
        {
            if (_acceptedOrRejected != null)
            {
                throw new InvalidOperationException(
                    $"Filter has already {(_acceptedDependency == null ? "rejected the" : "accepted a")} dependency.");
            }

            _acceptedOrRejected = false;
        }
    }
}
