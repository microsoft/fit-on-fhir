// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Microsoft.Health.FitOnFhir.Common.Handlers;
using Microsoft.Health.FitOnFhir.Common.Requests;

namespace Microsoft.Health.FitOnFhir.Common.Tests.Mocks
{
    internal class MockNextResponsibilityHandler : RequestHandlerBase<RoutingRequest, Task<IActionResult>>
    {
        public MockNextResponsibilityHandler()
        {
        }

        public override IEnumerable<string> HandledRoutes => new List<string>() { "nextHandlerPlatform" };

        public override Task<IActionResult> EvaluateRequest(RoutingRequest request)
        {
            return Task.Run<IActionResult>(() => new UnauthorizedResult());
        }
    }
}
