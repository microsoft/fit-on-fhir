// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Microsoft.Health.FitOnFhir.Common.Requests;

namespace Microsoft.Health.FitOnFhir.Common.Handlers
{
    public class UnknownAuthorizationHandler : OperationHandlerBase<RoutingRequest, Task<IActionResult>>
    {
        public override IEnumerable<string> HandledRoutes { get; }

        public override Task<IActionResult> Evaluate(RoutingRequest request)
        {
            return Task.Run<IActionResult>(() => new NotFoundResult());
        }
    }
}
