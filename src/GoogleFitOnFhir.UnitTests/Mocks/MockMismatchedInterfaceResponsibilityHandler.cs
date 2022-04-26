﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Health.Common.Handler;

namespace GoogleFitOnFhir.UnitTests.Mocks
{
    internal class MockMismatchedInterfaceResponsibilityHandler : IResponsibilityHandler<HttpRequest, Task<IActionResult>>
    {
        public MockMismatchedInterfaceResponsibilityHandler()
        {
        }

        public static IResponsibilityHandler<HttpRequest, Task<IActionResult>> Instance { get; } = new MockMismatchedInterfaceResponsibilityHandler();

        public Task<IActionResult> Evaluate(HttpRequest request)
        {
            return Task.Run<IActionResult>(() => new NotFoundResult());
        }
    }
}
