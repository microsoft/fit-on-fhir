// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FitOnFhir.Common.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Common.Handler;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace FitOnFhir.Authorization.Services
{
    public class RoutingService : IRoutingService
    {
        private readonly IResponsibilityHandler<RoutingRequest, Task<IActionResult>> _handler;
        private readonly ILogger _logger;

        public RoutingService(IResponsibilityHandler<RoutingRequest, Task<IActionResult>> handler, ILogger<RoutingService> logger)
        {
            _handler = EnsureArg.IsNotNull(handler);
            _logger = EnsureArg.IsNotNull(logger);
        }

        /// <inheritdoc/>
        public Task<IActionResult> RouteTo(HttpRequest req, ExecutionContext context, CancellationToken cancellationToken)
        {
            try
            {
                var routingRequest = new RoutingRequest(req, context, cancellationToken);
                return _handler.Evaluate(routingRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Task.FromResult<IActionResult>(new NotFoundObjectResult(ex.Message));
            }
        }
    }
}
