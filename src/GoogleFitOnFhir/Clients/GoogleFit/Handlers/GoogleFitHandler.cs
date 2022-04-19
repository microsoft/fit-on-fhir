﻿// -------------------------------------------------------------------------------------------------
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
        private IAuthService _authService;
        private IUsersService _usersService;
        private readonly ILogger<GoogleFitHandler> _logger;

        public GoogleFitHandler(IAuthService authService, IUsersService usersService, ILogger<GoogleFitHandler> logger)
        {
            _authService = authService;
            _usersService = usersService;
            _logger = logger;
        }

        public Task<IActionResult> Evaluate(RoutingRequest request)
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

        private async Task<IActionResult> Authorize(RoutingRequest request)
        {
            var token = EnsureArg.IsNotDefault(request.Token);
            AuthUriResponse response = await _authService.AuthUriRequest(token);
            return new RedirectResult(response.Uri);
        }

        private async Task<IActionResult> Callback(RoutingRequest request)
        {
            try
            {
                var accessCode = EnsureArg.IsNotNullOrWhiteSpace(request.HttpRequest.Query["code"]);
                var token = EnsureArg.IsNotDefault(request.Token);
                await _usersService.Initiate(accessCode, token);
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
