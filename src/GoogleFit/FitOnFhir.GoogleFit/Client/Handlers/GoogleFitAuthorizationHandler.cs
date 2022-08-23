// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Common.Handler;
using Microsoft.Health.FitOnFhir.Common;
using Microsoft.Health.FitOnFhir.Common.Exceptions;
using Microsoft.Health.FitOnFhir.Common.Handlers;
using Microsoft.Health.FitOnFhir.Common.Interfaces;
using Microsoft.Health.FitOnFhir.Common.Models;
using Microsoft.Health.FitOnFhir.Common.Requests;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Responses;
using Microsoft.Health.FitOnFhir.GoogleFit.Common;
using Microsoft.Health.FitOnFhir.GoogleFit.Services;
using Newtonsoft.Json;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Client.Handlers
{
    public class GoogleFitAuthorizationHandler : OperationHandlerBase<RoutingRequest, Task<IActionResult>>
    {
        private readonly IGoogleFitAuthService _authService;
        private readonly IUsersService _usersService;
        private readonly ITokenValidationService _tokenValidationService;
        private readonly IAuthStateService _authStateService;
        private readonly ILogger<GoogleFitAuthorizationHandler> _logger;
        private List<string> _handledRoutes;

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
            InitHandledRoutes();
        }

        public static IResponsibilityHandler<RoutingRequest, Task<IActionResult>> Instance { get; } = new GoogleFitAuthorizationHandler();

        public override IEnumerable<string> HandledRoutes => _handledRoutes;

        protected Func<string, bool> IsRouteHandled => (x) => { return HandledRoutes.Any(str => str == x); };

        public override Task<IActionResult> Evaluate(RoutingRequest request)
        {
            try
            {
                var route = EnsureArg.IsNotNullOrWhiteSpace(request.HttpRequest.Path.Value?[1..]);

                if (!IsRouteHandled(route))
                {
                    return null;
                }

                if (route.StartsWith(GoogleFitConstants.GoogleFitCallbackRequest))
                {
                    return Callback(request);
                }

                return HandleAuthorizeRequest(request, route);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Task.FromResult<IActionResult>(new ObjectResult("An unexpected error occurred while attempting to authorize access.") { StatusCode = StatusCodes.Status500InternalServerError });
            }
        }

        protected async Task<IActionResult> HandleAuthorizeRequest(RoutingRequest request, string route)
        {
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

            if (route.StartsWith(GoogleFitConstants.GoogleFitAuthorizeRequest))
            {
                return await Authorize(request, state);
            }

            if (route.StartsWith(GoogleFitConstants.GoogleFitRevokeAccessRequest))
            {
                return await Revoke(request, state);
            }

            _logger.LogError($"Route '{route}' among stored routes, but was not handled.");
            return await Task.FromResult<IActionResult>(null);
        }

        private async Task<IActionResult> Callback(RoutingRequest request)
        {
            try
            {
                var url = await _usersService.ProcessAuthorizationCallback(
                    request?.HttpRequest?.Query?["code"],
                    request?.HttpRequest?.Query?["state"],
                    request.Token);

                return new RedirectResult(url);
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
                return new JsonResult(JsonConvert.SerializeObject(new AuthorizeResponseData(response.Uri, state.ExpirationTimeStamp)))
                    { StatusCode = (int?)HttpStatusCode.OK };
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

        private void InitHandledRoutes()
        {
            _handledRoutes = new List<string>();
            _handledRoutes.Add(GoogleFitConstants.GoogleFitAuthorizeRequest);
            _handledRoutes.Add(GoogleFitConstants.GoogleFitRevokeAccessRequest);
            _handledRoutes.Add(GoogleFitConstants.GoogleFitCallbackRequest);
        }
    }
}
