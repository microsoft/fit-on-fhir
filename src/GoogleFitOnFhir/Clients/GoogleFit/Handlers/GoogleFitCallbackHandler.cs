// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using EnsureThat;
using GoogleFitOnFhir.Common;
using GoogleFitOnFhir.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Common.Handler;

namespace GoogleFitOnFhir.Clients.GoogleFit.Handlers
{
    public class GoogleFitCallbackHandler : IResponsibilityHandler<(IServiceScope scope, RoutingRequest request), Task<IActionResult>>
    {
        private const string GoogleFitCallbackRequest = "api/googlefit/callback";

        private GoogleFitCallbackHandler()
        {
        }

        public static IResponsibilityHandler<(IServiceScope scope, RoutingRequest request), Task<IActionResult>> Instance { get; } = new GoogleFitCallbackHandler();

        public Task<IActionResult> Evaluate((IServiceScope scope, RoutingRequest request) operation)
        {
            var path = EnsureArg.IsNotNull(operation.request.HttpRequest.Path.Value?[1..]);

            if (path.StartsWith(GoogleFitCallbackRequest))
            {
                return Callback(operation);
            }
            else
            {
                return null;
            }
        }

        public async Task<IActionResult> Callback((IServiceScope scope, RoutingRequest request) operation)
        {
            try
            {
                var usersService = operation.scope.ServiceProvider.GetRequiredService<IUsersService>();
                await usersService.Initiate(operation.request.HttpRequest.Query["code"], operation.request.Token);
                return new OkObjectResult("auth flow successful");
            }
            catch (Exception ex)
            {
                var logger = operation.scope.ServiceProvider.GetRequiredService<ILogger<GoogleFitCallbackHandler>>();
                logger.LogError(ex.Message);
                return new NotFoundObjectResult(ex.Message);
            }
        }
    }
}
