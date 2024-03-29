﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.AspNetCore.Http;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace Microsoft.Health.FitOnFhir.Common.Requests
{
    public class RoutingRequest : RequestBase
    {
        public RoutingRequest(HttpRequest httpRequest, ExecutionContext context, CancellationToken token)
        {
            HttpRequest = EnsureArg.IsNotNull(httpRequest);
            Context = EnsureArg.IsNotNull(context);
            Token = token;
        }

        public HttpRequest HttpRequest { get; set; }

        public ExecutionContext Context { get; set; }

        public CancellationToken Token { get; set; }

        public override string Route => HttpRequest.Path.Value?[1..];
    }
}
