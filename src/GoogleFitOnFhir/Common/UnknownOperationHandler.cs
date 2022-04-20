// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Health.Common.Handler;

namespace GoogleFitOnFhir.Common
{
    public class UnknownOperationHandler : IResponsibilityHandler<RoutingRequest, Task<IActionResult>>
    {
        public UnknownOperationHandler()
        {
        }

        public static IResponsibilityHandler<RoutingRequest, Task<IActionResult>> Instance { get; } = new UnknownOperationHandler();

        public Task<IActionResult> Evaluate(RoutingRequest request)
        {
            return Task.Run<IActionResult>(() => new NotFoundResult());
        }
    }
}
