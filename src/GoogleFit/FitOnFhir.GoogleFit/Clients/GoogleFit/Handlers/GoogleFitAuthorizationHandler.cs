// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using EnsureThat;
using FitOnFhir.Common.Requests;
using FitOnFhir.GoogleFit.Clients.GoogleFit.Responses;
using FitOnFhir.GoogleFit.Common;
using FitOnFhir.GoogleFit.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Common.Handler;

namespace FitOnFhir.GoogleFit.Clients.GoogleFit.Handlers
{
    public class GoogleFitAuthorizationHandler : IResponsibilityHandler<RoutingRequest, Task<IActionResult>>
    {
        private readonly IGoogleFitAuthService _authService;
        private readonly IUsersService _usersService;
        private readonly ILogger<GoogleFitAuthorizationHandler> _logger;

        private GoogleFitAuthorizationHandler()
        {
        }

        public GoogleFitAuthorizationHandler(IGoogleFitAuthService authService, IUsersService usersService, ILogger<GoogleFitAuthorizationHandler> logger)
        {
            _authService = EnsureArg.IsNotNull(authService);
            _usersService = EnsureArg.IsNotNull(usersService);
            _logger = EnsureArg.IsNotNull(logger);
        }

        public static IResponsibilityHandler<RoutingRequest, Task<IActionResult>> Instance { get; } = new GoogleFitAuthorizationHandler();

        public Task<IActionResult> Evaluate(RoutingRequest request)
        {
            try
            {
                var path = EnsureArg.IsNotNullOrWhiteSpace(request.HttpRequest.Path.Value?[1..]);

                if (path.StartsWith(Constants.GoogleFitAuthorizeRequest))
                {
                    return Authorize(request);
                }
                else if (path.StartsWith(Constants.GoogleFitCallbackRequest))
                {
                    return Callback(request);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        private async Task<IActionResult> Authorize(RoutingRequest request)
        {
            AuthUriResponse response = await _authService.AuthUriRequest(request.Token);
            return new RedirectResult(response.Uri);
        }

        private async Task<IActionResult> Callback(RoutingRequest request)
        {
            try
            {
                var accessCode = EnsureArg.IsNotNullOrWhiteSpace(request?.HttpRequest?.Query?["code"], "accessCode");
                await _usersService.Initiate(accessCode, request.Token);
                return new OkObjectResult("auth flow success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new NotFoundObjectResult(ex.Message);
            }
        }
    }
}
