// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.FitOnFhir.Common.Requests
{
    public abstract class RequestBase
    {
        /// <summary>
        /// Used to determine whether a given handler can handle the request.
        /// </summary>
        public abstract string Route { get; }
    }
}
