// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using GoogleFitOnFhir.Clients.GoogleFit.Handlers;
using GoogleFitOnFhir.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Common.Handler;

namespace GoogleFitOnFhir.Services
{
    public class RoutingService : IRoutingService
    {
        private readonly IServiceScope _serviceScope;
        private readonly IResponsibilityHandler<(IServiceScope scope, RoutingRequest request), Task<IActionResult>> _handler;
        private readonly ILogger _logger;

        public RoutingService(IServiceScope scope, IResponsibilityHandler<(IServiceScope scope, RoutingRequest request), Task<IActionResult>> handler, ILogger<RoutingService> logger)
        {
            _serviceScope = scope;
            _handler = handler;
            _logger = logger;

            _handler = handler;
            BuildHandlerChain();
        }

        public Task<IActionResult> RouteTo(HttpRequest req, string root, CancellationToken cancellationToken)
        {
            var routingRequest = new RoutingRequest() { HttpRequest = req, Root = root, Token = cancellationToken };
            return _handler.Evaluate((_serviceScope, routingRequest));
        }

        private void BuildHandlerChain()
        {
            _handler.Chain(GoogleFitAuthorizationHandler.Instance)
                .Chain(GoogleFitCallbackHandler.Instance)
                .Chain(GoogleFitFileHandler.Instance)
                .Chain(UnknownOperationHandler.Instance);
        }
    }
}
