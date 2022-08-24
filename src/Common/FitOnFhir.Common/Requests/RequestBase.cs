// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;

namespace Microsoft.Health.FitOnFhir.Common.Requests
{
    public abstract class RequestBase
    {
        /// <summary>
        /// The path component of an <see cref="HttpRequest"/> that this request is targeted for.
        /// </summary>
        public abstract string Route { get; }
    }
}
