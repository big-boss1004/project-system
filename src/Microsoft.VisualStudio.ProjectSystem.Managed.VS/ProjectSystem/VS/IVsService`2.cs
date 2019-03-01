﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides access to a Visual Studio proffered service.
    /// </summary>
    /// <typeparam name="TService">
    ///     The type of the service to retrieve.
    /// </typeparam>
    /// <typeparam name="TInterface">
    ///     The type of the service to return from <see cref="IVsService{T}.GetValueAsync(CancellationToken)"/>
    /// </typeparam>
    [ProjectSystemContract(ProjectSystemContractScope2.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IVsService<TService, TInterface> : IVsService<TInterface>
    {
    }
}
