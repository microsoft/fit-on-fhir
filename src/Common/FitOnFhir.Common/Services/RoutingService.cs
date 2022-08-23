// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Common.Handler;
using Microsoft.Health.FitOnFhir.Common.Requests;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace Microsoft.Health.FitOnFhir.Common.Services
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
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, req.HttpContext.RequestAborted);

            try
            {
                var routingRequest = new RoutingRequest(req, context, cancellationSource.Token);
                return _handler.Evaluate(routingRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Task.FromResult<IActionResult>(new ObjectResult("An unexpected internal error occurred.") { StatusCode = StatusCodes.Status500InternalServerError });
            }
        }
    }
}
