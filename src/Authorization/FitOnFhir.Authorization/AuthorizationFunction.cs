﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FitOnFhir.Authorization.Services;
using FitOnFhir.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace FitOnFhir.Authorization
{
    public class AuthorizationFunction
    {
        private readonly IRoutingService _routingService;
        private readonly ILogger _logger;

        public AuthorizationFunction(
            IRoutingService routingService,
            ITokenValidationService authenticationHandler,
            ILogger<AuthorizationFunction> logger)
        {
            _routingService = EnsureArg.IsNotNull(routingService, nameof(routingService));
            _logger = EnsureArg.IsNotNull(logger);
        }

        [FunctionName("api")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{p1?}/{p2?}/{p3?}")] HttpRequest req,
            ExecutionContext context,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("incoming request from: {0}", req.Host + req.Path);
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, req.HttpContext.RequestAborted);

            return await _routingService.RouteTo(req, context, cancellationSource.Token);
        }
    }
}
