﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class FileItemServices
    {
        public static string GetLinkFilePath(IImmutableDictionary<string, string> metadata)
        {
            Requires.NotNull(metadata, nameof(metadata));

            // This mimic's CPS's handling of Link metadata
            if (metadata.TryGetValue(Compile.LinkProperty, out string linkFilePath) && !string.IsNullOrWhiteSpace(linkFilePath))
            {
                return linkFilePath.TrimEnd(Delimiter.Path);
            }

            return null;
        }

    }
}
