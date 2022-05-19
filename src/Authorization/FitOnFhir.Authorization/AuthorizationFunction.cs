// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FitOnFhir.Authorization.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace FitOnFhir.Authorization
{
    public class AuthorizationFunction
    {
        private readonly IRoutingService _routingService;
        private readonly ILogger _logger;

        public AuthorizationFunction(IRoutingService routingService, ILogger<AuthorizationFunction> logger)
        {
            _routingService = EnsureArg.IsNotNull(routingService);
            _logger = EnsureArg.IsNotNull(logger);
        }

        [FunctionName("authorize")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{p1?}/{p2?}/{p3?}")] HttpRequest req,
            Microsoft.Azure.WebJobs.ExecutionContext context,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("incoming request from: {0}", req.Host + req.Path);
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, req.HttpContext.RequestAborted);
            return await _routingService.RouteTo(req, context, cancellationSource.Token);
        }
    }
}
