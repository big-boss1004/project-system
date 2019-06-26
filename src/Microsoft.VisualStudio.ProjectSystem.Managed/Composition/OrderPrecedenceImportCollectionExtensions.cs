﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.Composition
{
    internal static class OrderPrecedenceImportCollectionExtensions
    {
        /// <summary>
        /// Produces the sequence of imports within a <see cref="OrderPrecedenceImportCollection{T}"/>,
        /// omitting any that throw MEF exceptions.
        /// </summary>
        /// <typeparam name="T">The type of import.</typeparam>
        /// <param name="extensions">The collection of imports.</param>
        /// <param name="onlyCreatedValues">
        /// <c>true</c> to only enumerate imports whose values have previously been created.
        /// This is useful in <see cref="IDisposable.Dispose"/> methods to avoid MEF
        /// <see cref="ObjectDisposedException"/> from accidentally creating values during a container disposal.
        /// </param>
        /// <returns>The safely constructed sequence of extensions.</returns>
        internal static IEnumerable<T> ExtensionValues<T>(this OrderPrecedenceImportCollection<T> extensions, bool onlyCreatedValues = false)
        {
            Requires.NotNull(extensions, nameof(extensions));
            string traceErrorMessage = "Roslyn project system extension rejected due to exception: {0}";

            foreach (Lazy<T> extension in extensions)
            {
                T value;
                try
                {
                    if (onlyCreatedValues && !extension.IsValueCreated)
                    {
                        continue;
                    }

                    value = extension.Value;
                }
                catch (CompositionContractMismatchException ex)
                {
                    TraceUtilities.TraceError(traceErrorMessage, ex);
                    continue;
                }
                catch (CompositionException ex)
                {
                    TraceUtilities.TraceError(traceErrorMessage, ex);
                    continue;
                }

                yield return value;
            }
        }

        public static T? FirstOrDefaultValue<T>(this OrderPrecedenceImportCollection<T> imports, Func<T, bool> predicate) where T : class
        {
            Requires.NotNull(imports, nameof(imports));

            foreach (Lazy<T> import in imports)
            {
                T value = import.Value;
                if (predicate(value))
                {
                    return value;
                }
            }

            return default;
        }

        public static TImport? FirstOrDefaultValue<TImport, TArg>(this OrderPrecedenceImportCollection<TImport> imports, Func<TImport, TArg, bool> predicate, TArg arg) where TImport : class
        {
            Requires.NotNull(imports, nameof(imports));

            foreach (Lazy<TImport> import in imports)
            {
                TImport value = import.Value;
                if (predicate(value, arg))
                {
                    return value;
                }
            }

            return default;
        }

        public static ImmutableArray<T> ToImmutableValueArray<T>(this OrderPrecedenceImportCollection<T> imports)
        {
            Requires.NotNull(imports, nameof(imports));

            ImmutableArray<T>.Builder builder = ImmutableArray.CreateBuilder<T>(imports.Count);

            foreach (Lazy<T> import in imports)
            {
                builder.Add(import.Value);
            }

            return builder.MoveToImmutable();
        }

        public static Dictionary<TKey, TImport> ToValueDictionary<TKey, TImport>(this OrderPrecedenceImportCollection<TImport> imports, Func<TImport, TKey> keySelector, IEqualityComparer<TKey>? comparer = default)
        {
            var dictionary = new Dictionary<TKey, TImport>(comparer);

            foreach (Lazy<TImport> import in imports)
            {
                TImport value = import.Value;
                dictionary.Add(keySelector(value), value);
            }

            return dictionary;
        }
    }
}
