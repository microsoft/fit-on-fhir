// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Common.Handler;
using Microsoft.Health.FitOnFhir.Common;
using Microsoft.Health.FitOnFhir.Common.Exceptions;
using Microsoft.Health.FitOnFhir.Common.Interfaces;
using Microsoft.Health.FitOnFhir.Common.Models;
using Microsoft.Health.FitOnFhir.Common.Requests;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Responses;
using Microsoft.Health.FitOnFhir.GoogleFit.Common;
using Microsoft.Health.FitOnFhir.GoogleFit.Services;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Client.Handlers
{
    public class GoogleFitAuthorizationHandler : IResponsibilityHandler<RoutingRequest, Task<IActionResult>>
    {
        private readonly IGoogleFitAuthService _authService;
        private readonly IUsersService _usersService;
        private readonly ITokenValidationService _tokenValidationService;
        private readonly IAuthStateService _authStateService;
        private readonly ILogger<GoogleFitAuthorizationHandler> _logger;

        private GoogleFitAuthorizationHandler()
        {
        }

        public GoogleFitAuthorizationHandler(
            IGoogleFitAuthService authService,
            IUsersService usersService,
            ITokenValidationService tokenValidationService,
            IAuthStateService authStateService,
            ILogger<GoogleFitAuthorizationHandler> logger)
        {
            _authService = EnsureArg.IsNotNull(authService, nameof(authService));
            _usersService = EnsureArg.IsNotNull(usersService, nameof(usersService));
            _tokenValidationService = EnsureArg.IsNotNull(tokenValidationService, nameof(tokenValidationService));
            _authStateService = EnsureArg.IsNotNull(authStateService, nameof(authStateService));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        public static IResponsibilityHandler<RoutingRequest, Task<IActionResult>> Instance { get; } = new GoogleFitAuthorizationHandler();

        public async Task<IActionResult> Evaluate(RoutingRequest request)
        {
            try
            {
                var path = EnsureArg.IsNotNullOrWhiteSpace(request.HttpRequest.Path.Value?[1..]);

                if (path.StartsWith(GoogleFitConstants.GoogleFitCallbackRequest))
                {
                    return await Callback(request);
                }

                AuthState state;
                var isValidated = await _tokenValidationService.ValidateToken(request.HttpRequest, request.Token);
                if (isValidated)
                {
                    try
                    {
                        state = _authStateService.CreateAuthState(request.HttpRequest);
                    }
                    catch (ArgumentException)
                    {
                        return new BadRequestObjectResult($"'{Constants.ExternalIdQueryParameter}' and '{Constants.ExternalSystemQueryParameter}' are required query parameters.");
                    }
                    catch (RedirectUrlException ex)
                    {
                        return new BadRequestObjectResult(ex.Message);
                    }
                }
                else
                {
                    return new UnauthorizedResult();
                }

                if (path.StartsWith(GoogleFitConstants.GoogleFitAuthorizeRequest))
                {
                    return await Authorize(request, state);
                }

                if (path.StartsWith(GoogleFitConstants.GoogleFitRevokeAccessRequest))
                {
                    return await Revoke(request, state);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
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

        private async Task<IActionResult> Authorize(RoutingRequest request, AuthState state)
        {
            try
            {
                var nonce = await _authStateService.StoreAuthState(state, request.Token);
                AuthUriResponse response = await _authService.AuthUriRequest(nonce, request.Token);
                return new RedirectResult(response.Uri);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new ObjectResult("An unexpected error occurred while attempting to authorize access.") { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        private async Task<IActionResult> Revoke(RoutingRequest request, AuthState state)
        {
            try
            {
                await _usersService.RevokeAccess(state, request.Token);
                return new OkObjectResult("Access revoked successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new ObjectResult("An unexpected error occurred while attempting to revoke data access.") { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }
    }
}
