// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.Common.Requests;
using Microsoft.AspNetCore.Mvc;

namespace FitOnFhir.Common.Handlers
{
    public class UnknownAuthorizationHandler : UnknownOperationHandlerBase<RoutingRequest, Task<IActionResult>>
    {
        public override Task<IActionResult> Evaluate(RoutingRequest request)
        {
            return Task.Run<IActionResult>(() => new NotFoundResult());
        }
    }
}
