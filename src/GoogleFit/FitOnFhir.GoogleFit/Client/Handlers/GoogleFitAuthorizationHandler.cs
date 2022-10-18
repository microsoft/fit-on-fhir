// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using EnsureThat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.FitOnFhir.Common.Exceptions;
using Microsoft.Health.FitOnFhir.Common.Handlers;
using Microsoft.Health.FitOnFhir.Common.Interfaces;
using Microsoft.Health.FitOnFhir.Common.Models;
using Microsoft.Health.FitOnFhir.Common.Requests;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Responses;
using Microsoft.Health.FitOnFhir.GoogleFit.Common;
using Microsoft.Health.FitOnFhir.GoogleFit.Services;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Client.Handlers
{
    public class GoogleFitAuthorizationHandler : RequestHandlerBase<RoutingRequest, Task<IActionResult>>
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

        public override IEnumerable<string> HandledRoutes => new List<string>()
        {
            GoogleFitConstants.GoogleFitAuthorizeRequest,
            GoogleFitConstants.GoogleFitRevokeAccessRequest,
            GoogleFitConstants.GoogleFitCallbackRequest,
        };

        public override Task<IActionResult> EvaluateRequest(RoutingRequest request)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            if (request.Route.StartsWith(GoogleFitConstants.GoogleFitCallbackRequest, StringComparison.OrdinalIgnoreCase))
            {
                return Callback(request);
            }

            return HandleAuthorizeRequest(request);
        }

        protected async Task<IActionResult> HandleAuthorizeRequest(RoutingRequest request)
        {
            EnsureArg.IsNotNull(EnsureArg.IsNotNull(request, nameof(request)));

            AuthState state;
            var isValidated = await _tokenValidationService.ValidateToken(request.HttpRequest, request.Token);
            if (isValidated)
            {
                try
                {
                    state = _authStateService.CreateAuthState(request.HttpRequest);
                }
                catch (AuthStateException ex)
                {
                    _logger.LogError(ex, ex.Message);
                    return new BadRequestObjectResult(ex.Message);
                }
            }
            else
            {
                return new UnauthorizedResult();
            }

            if (request.Route.StartsWith(GoogleFitConstants.GoogleFitAuthorizeRequest, StringComparison.OrdinalIgnoreCase))
            {
                return await Authorize(request, state);
            }

            if (request.Route.StartsWith(GoogleFitConstants.GoogleFitRevokeAccessRequest, StringComparison.OrdinalIgnoreCase))
            {
                return await Revoke(request, state);
            }

            _logger.LogError($"Route '{request.Route}' among stored routes, but was not handled.");
            return null;
        }

        private async Task<IActionResult> Callback(RoutingRequest request)
        {
            var url = await _usersService.ProcessAuthorizationCallback(
                request?.HttpRequest?.Query?["code"],
                request?.HttpRequest?.Query?["state"],
                request.Token);

            return new RedirectResult(url.ToString());
        }

        private async Task<IActionResult> Authorize(RoutingRequest request, AuthState state)
        {
            var nonce = await _authStateService.StoreAuthState(state, request.Token);
            AuthUriResponse response = await _authService.AuthUriRequest(nonce, request.Token);
            return new JsonResult(new AuthorizeResponseData(response.Uri, state.ExpirationTimeStamp))
                { StatusCode = (int?)HttpStatusCode.OK };
        }

        private async Task<IActionResult> Revoke(RoutingRequest request, AuthState state)
        {
            await _usersService.RevokeAccess(state, request.Token);
            return new OkResult();
        }
    }
}
