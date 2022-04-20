// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using GoogleFitOnFhir.Clients.GoogleFit.Handlers;
using GoogleFitOnFhir.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Common.Handler;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace GoogleFitOnFhir.Services
{
    public class RoutingService : IRoutingService
    {
        private readonly IServiceScope _serviceScope;
        private readonly IResponsibilityHandler<RoutingRequest, Task<IActionResult>> _handler;
        private readonly ILogger _logger;

        public RoutingService(IServiceScope scope, IResponsibilityHandler<RoutingRequest, Task<IActionResult>> handler, ILogger<RoutingService> logger)
        {
            _serviceScope = scope;
            _handler = handler;
            _logger = logger;

            BuildHandlerChain();
        }

        public Task<IActionResult> RouteTo(HttpRequest req, ExecutionContext context, CancellationToken cancellationToken)
        {
            try
            {
                var routingRequest = new RoutingRequest() { HttpRequest = req, Context = context, Token = cancellationToken };
                return _handler.Evaluate(routingRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Task.FromResult<IActionResult>(new NotFoundObjectResult(ex.Message));
            }
        }

        private void BuildHandlerChain()
        {
            _handler.Chain(new GoogleFitHandler(
                    _serviceScope.ServiceProvider.GetRequiredService<IAuthService>(),
                    _serviceScope.ServiceProvider.GetRequiredService<IUsersService>(),
                    _serviceScope.ServiceProvider.GetRequiredService<ILogger<GoogleFitHandler>>()))
                .Chain(UnknownOperationHandler.Instance);
        }
    }
}
