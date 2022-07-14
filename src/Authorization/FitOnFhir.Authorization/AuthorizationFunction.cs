// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FitOnFhir.Authorization.Handlers;
using FitOnFhir.Authorization.Services;
using FitOnFhir.Common.Config;
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
        private readonly IFitOnFhirAuthenticationHandler _authenticationHandler;
        private readonly AuthenticationConfiguration _authenticationConfiguration;
        private readonly ILogger _logger;

        public AuthorizationFunction(
            IRoutingService routingService,
            IFitOnFhirAuthenticationHandler authenticationHandler,
            AuthenticationConfiguration authenticationConfiguration,
            ILogger<AuthorizationFunction> logger)
        {
            _routingService = EnsureArg.IsNotNull(routingService, nameof(routingService));
            _authenticationHandler = EnsureArg.IsNotNull(authenticationHandler, nameof(authenticationHandler));
            _authenticationConfiguration = EnsureArg.IsNotNull(authenticationConfiguration, nameof(authenticationConfiguration));
            _logger = EnsureArg.IsNotNull(logger);
        }

        [FunctionName("api")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{p1?}/{p2?}/{p3?}")] HttpRequest req,
            ExecutionContext context,
            CancellationToken cancellationToken)
        {
            bool isAuthenticated = true;

            _logger.LogInformation("incoming request from: {0}", req.Host + req.Path);
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, req.HttpContext.RequestAborted);

            if (!_authenticationConfiguration.IsAnonymousLoginEnabled)
            {
                // populate the issuer dictionary
                await _authenticationHandler.CreateIssuerMapping(cancellationSource.Token);

                // authenticate the token
                isAuthenticated = await _authenticationHandler.AuthenticateToken(req, cancellationSource.Token);
            }

            if (isAuthenticated)
            {
                return await _routingService.RouteTo(req, context, cancellationSource.Token);
            }

            return new UnauthorizedResult();
        }
    }
}
