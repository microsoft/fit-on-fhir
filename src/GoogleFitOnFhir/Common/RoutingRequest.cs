// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using Microsoft.AspNetCore.Http;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace GoogleFitOnFhir.Common
{
    public class RoutingRequest
    {
        public HttpRequest HttpRequest { get; set; }

        public ExecutionContext Context { get; set; }

        public CancellationToken Token { get; set; }
    }
}
