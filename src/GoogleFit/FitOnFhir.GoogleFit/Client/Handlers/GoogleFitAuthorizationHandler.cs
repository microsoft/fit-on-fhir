// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using FitOnFhir.Common;
using FitOnFhir.Common.Interfaces;
using FitOnFhir.Common.Models;
using FitOnFhir.Common.Requests;
using FitOnFhir.GoogleFit.Client.Responses;
using FitOnFhir.GoogleFit.Common;
using FitOnFhir.GoogleFit.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Common.Handler;
using Newtonsoft.Json;

namespace FitOnFhir.GoogleFit.Client.Handlers
{
    public class GoogleFitAuthorizationHandler : IResponsibilityHandler<RoutingRequest, Task<IActionResult>>
    {
        private readonly IGoogleFitAuthService _authService;
        private readonly IUsersService _usersService;
        private readonly ITokenValidationService _tokenValidationService;
        private readonly ILogger<GoogleFitAuthorizationHandler> _logger;

        private GoogleFitAuthorizationHandler()
        {
        }

        public GoogleFitAuthorizationHandler(
            IGoogleFitAuthService authService,
            IUsersService usersService,
            ITokenValidationService tokenValidationService,
            ILogger<GoogleFitAuthorizationHandler> logger)
        {
            _authService = EnsureArg.IsNotNull(authService, nameof(authService));
            _usersService = EnsureArg.IsNotNull(usersService, nameof(usersService));
            _tokenValidationService = EnsureArg.IsNotNull(tokenValidationService, nameof(tokenValidationService));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        public static IResponsibilityHandler<RoutingRequest, Task<IActionResult>> Instance { get; } = new GoogleFitAuthorizationHandler();

        public Task<IActionResult> Evaluate(RoutingRequest request)
        {
            try
            {
                var path = EnsureArg.IsNotNullOrWhiteSpace(request.HttpRequest.Path.Value?[1..]);

                if (path.StartsWith(GoogleFitConstants.GoogleFitAuthorizeRequest))
                {
                    return Authorize(request);
                }
                else if (path.StartsWith(GoogleFitConstants.GoogleFitCallbackRequest))
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
            AuthState state = null;

            var isValidated = await _tokenValidationService.ValidateToken(request.HttpRequest, request.Token);

            if (isValidated)
            {
                try
                {
                    state = new AuthState(request?.HttpRequest?.Query);
                }
                catch (ArgumentException)
                {
                    return new BadRequestObjectResult($"'{Constants.PatientIdQueryParameter}' and '{Constants.SystemQueryParameter}' are required query parameters.");
                }

                AuthUriResponse response = await _authService.AuthUriRequest(JsonConvert.SerializeObject(state), request.Token);
                return new RedirectResult(response.Uri);
            }
            else
            {
                return new UnauthorizedResult();
            }
        }

        private async Task<IActionResult> Callback(RoutingRequest request)
        {
            try
            {
                await _usersService.ProcessAuthorizationCallback(
                    request?.HttpRequest?.Query?["code"],
                    request?.HttpRequest?.Query?["state"],
                    request.Token);

                return new OkObjectResult("Authorization completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new NotFoundObjectResult(ex.Message);
            }
        }
    }
}
