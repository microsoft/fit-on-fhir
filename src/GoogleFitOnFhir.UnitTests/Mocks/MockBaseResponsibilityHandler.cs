// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using GoogleFitOnFhir.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Health.Common.Handler;

namespace GoogleFitOnFhir.UnitTests.Mocks
{
    internal class MockBaseResponsibilityHandler : IResponsibilityHandler<RoutingRequest, Task<IActionResult>>
    {
        public MockBaseResponsibilityHandler()
        {
        }

        public static IResponsibilityHandler<RoutingRequest, Task<IActionResult>> Instance { get; } = new MockBaseResponsibilityHandler();

        public Task<IActionResult> Evaluate(RoutingRequest request)
        {
            return Task.Run<IActionResult>(() => new OkResult());
        }
    }
}
