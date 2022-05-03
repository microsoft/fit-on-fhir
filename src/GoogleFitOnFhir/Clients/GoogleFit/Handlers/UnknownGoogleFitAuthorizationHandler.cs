// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using GoogleFitOnFhir.Common;
using Microsoft.AspNetCore.Mvc;

namespace GoogleFitOnFhir.Clients.GoogleFit.Handlers
{
    public class UnknownGoogleFitAuthorizationHandler : UnknownOperationHandlerBase<RoutingRequest, Task<IActionResult>>
    {
        public override Task<IActionResult> Evaluate(RoutingRequest request)
        {
            return Task.Run<IActionResult>(() => new NotFoundResult());
        }
    }
}
