// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using EnsureThat;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;
using GoogleFitOnFhir.Common;
using GoogleFitOnFhir.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Common.Handler;

namespace GoogleFitOnFhir.Clients.GoogleFit.Handlers
{
    public class GoogleFitHandler : IResponsibilityHandler<RoutingRequest, Task<IActionResult>>
    {
        private const string GoogleFitAuthorizeRequest = "api/googlefit/authorize";
        private const string GoogleFitCallbackRequest = "api/googlefit/callback";
        private readonly IAuthService _authService;
        private readonly IUsersService _usersService;
        private readonly ILogger<GoogleFitHandler> _logger;

        private GoogleFitHandler()
        {
        }

        public GoogleFitHandler(IAuthService authService, IUsersService usersService, ILogger<GoogleFitHandler> logger)
        {
            _authService = EnsureArg.IsNotNull(authService);
            _usersService = EnsureArg.IsNotNull(usersService);
            _logger = EnsureArg.IsNotNull(logger);
        }

        public static IResponsibilityHandler<RoutingRequest, Task<IActionResult>> Instance { get; } = new GoogleFitHandler();

        public Task<IActionResult> Evaluate(RoutingRequest request)
        {
            try
            {
                var path = EnsureArg.IsNotNullOrWhiteSpace(request.HttpRequest.Path.Value?[1..]);

                if (path.StartsWith(GoogleFitAuthorizeRequest))
                {
                    return Authorize(request);
                }
                else if (path.StartsWith(GoogleFitCallbackRequest))
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
                _logger.LogError(ex.Message);
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
                _logger.LogError(ex.Message);
                return new NotFoundObjectResult(ex.Message);
            }
        }
    }
}
