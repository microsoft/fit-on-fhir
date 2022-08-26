// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Microsoft.Health.FitOnFhir.Common.Handlers;
using Microsoft.Health.FitOnFhir.Common.Requests;

namespace Microsoft.Health.FitOnFhir.Common.Tests.Mocks
{
    internal class MockResponsibilityHandler<TResult> : RequestHandlerBase<RoutingRequest, Task<IActionResult>>
    {
        private readonly string _route;

        public MockResponsibilityHandler(string route)
        {
            _route = route;
        }

        public override IEnumerable<string> HandledRoutes => new List<string>() { _route };

        public override Task<IActionResult> EvaluateRequest(RoutingRequest request)
        {
            return Task.FromResult<IActionResult>((IActionResult)Activator.CreateInstance(typeof(TResult)));
        }
    }
}
