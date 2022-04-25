// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using GoogleFitOnFhir.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace GoogleFitOnFhir.Identity
{
    public class IdentityFunction
    {
        private readonly IRoutingService _routingService;
        private readonly ILogger _logger;

        public IdentityFunction(IRoutingService routingService, ILogger<IdentityFunction> logger)
        {
            _routingService = EnsureArg.IsNotNull(routingService);
            _logger = EnsureArg.IsNotNull(logger);
        }

        [FunctionName("api")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{p1?}/{p2?}/{p3?}")] HttpRequest req,
            Microsoft.Azure.WebJobs.ExecutionContext context,
            CancellationToken cancellationToken)
        {
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, req.HttpContext.RequestAborted);
            return await _routingService.RouteTo(req, context, cancellationSource.Token);
        }
    }
}
