// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Microsoft.Health.FitOnFhir.Common.Handlers;
using Microsoft.Health.FitOnFhir.Common.Requests;

namespace Microsoft.Health.FitOnFhir.Common.Tests.Mocks
{
    internal class MockFirstResponsibilityHandler : RequestHandlerBase<RoutingRequest, Task<IActionResult>>
    {
        public MockFirstResponsibilityHandler()
        {
        }

        public override IEnumerable<string> HandledRoutes => new List<string>() { "firstHandlerPlatform" };

        public override Task<IActionResult> EvaluateRequest(RoutingRequest request)
        {
            return Task.Run<IActionResult>(() => new OkResult());
        }
    }
}
