// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Microsoft.Health.Common.Handler;
using Microsoft.Health.FitOnFhir.Common.Requests;

namespace Microsoft.Health.FitOnFhir.Common.Handlers
{
    public class UnknownAuthorizationHandler : IResponsibilityHandler<RoutingRequest, Task<IActionResult>>
    {
        public Task<IActionResult> Evaluate(RoutingRequest request)
        {
            return Task.FromResult(new NotFoundResult() as IActionResult);
        }
    }
}
