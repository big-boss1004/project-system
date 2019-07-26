﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    internal static class ObservableListExtensions
    {
        public static ObservableList<NameValuePair> CreateList(this IDictionary<string, string> dictionary)
        {
            var list = new ObservableList<NameValuePair>();

            foreach (KeyValuePair<string, string> kvp in dictionary)
            {
                list.Add(new NameValuePair(kvp.Key, kvp.Value, list));
            }

            return list;
        }
    }
}
