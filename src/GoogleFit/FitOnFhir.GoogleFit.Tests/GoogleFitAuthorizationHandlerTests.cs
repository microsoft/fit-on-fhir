// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Common.Handler;
using Microsoft.Health.FitOnFhir.Common;
using Microsoft.Health.FitOnFhir.Common.Exceptions;
using Microsoft.Health.FitOnFhir.Common.Interfaces;
using Microsoft.Health.FitOnFhir.Common.Models;
using Microsoft.Health.FitOnFhir.Common.Requests;
using Microsoft.Health.FitOnFhir.Common.Tests.Mocks;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Handlers;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Responses;
using Microsoft.Health.FitOnFhir.GoogleFit.Common;
using Microsoft.Health.FitOnFhir.GoogleFit.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Tests
{
    public class GoogleFitAuthorizationHandlerTests
    {
        private readonly IResponsibilityHandler<RoutingRequest, Task<IActionResult>> _googleFitAuthorizationHandler;

        private readonly PathString googleFitAuthorizeRequest = "/" + GoogleFitConstants.GoogleFitAuthorizeRequest;
        private readonly PathString googleFitCallbackRequest = "/" + GoogleFitConstants.GoogleFitCallbackRequest;
        private readonly PathString googleFitRevokeRequest = "/" + GoogleFitConstants.GoogleFitRevokeAccessRequest;
        private readonly PathString emptyGoogleFitRequest = "/api/googlefit/";

        private static string _fakeRedirectUri = "http://localhost";

        private readonly IGoogleFitAuthService _authService;
        private readonly IUsersService _usersService;
        private readonly ITokenValidationService _tokenValidationService;
        private readonly IAuthStateService _authStateService;
        private readonly MockLogger<GoogleFitAuthorizationHandler> _logger;

        public GoogleFitAuthorizationHandlerTests()
        {
            _authService = Substitute.For<IGoogleFitAuthService>();
            _usersService = Substitute.For<IUsersService>();
            _tokenValidationService = Substitute.For<ITokenValidationService>();
            _authStateService = Substitute.For<IAuthStateService>();
            _logger = Substitute.For<MockLogger<GoogleFitAuthorizationHandler>>();

            _googleFitAuthorizationHandler = new GoogleFitAuthorizationHandler(
                _authService,
                _usersService,
                _tokenValidationService,
                _authStateService,
                _logger);

            _tokenValidationService.ValidateToken(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>()).Returns(true);
        }

        [Fact]
        public async Task GivenRequestCannotBeHandled_WhenEvaluateIsCalled_NullIsReturned()
        {
            var routingRequest = CreateRoutingRequest(emptyGoogleFitRequest);
            var result = await _googleFitAuthorizationHandler.Evaluate(routingRequest);

            Assert.Null(result);
        }

        [InlineData("/" + GoogleFitConstants.GoogleFitAuthorizeRequest)]
        [InlineData("/" + GoogleFitConstants.GoogleFitRevokeAccessRequest)]
        [Theory]
        public async Task GivenCreateAuthStateThrowsArgumentException_WhenRequestIsNotCallback_ReturnsBadRequestObjectResult(string request)
        {
            _authStateService.CreateAuthState(Arg.Any<HttpRequest>()).Throws(new ArgumentException("exception"));

            PathString requestPath = new PathString(request);
            var routingRequest = CreateRoutingRequest(requestPath, false, false);
            var result = await _googleFitAuthorizationHandler.Evaluate(routingRequest);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<string>(((BadRequestObjectResult)result).Value);
            Assert.Equal($"'{Constants.ExternalIdQueryParameter}' and '{Constants.ExternalSystemQueryParameter}' are required query parameters.", ((BadRequestObjectResult)result).Value);
        }

        [InlineData("/" + GoogleFitConstants.GoogleFitAuthorizeRequest)]
        [InlineData("/" + GoogleFitConstants.GoogleFitRevokeAccessRequest)]
        [Theory]
        public async Task GivenCreateAuthStateThrowsRedirectUrlException_WhenRequestIsNotCallback_ReturnsBadRequestObjectResult(string request)
        {
            string exceptionMessage = "failed to redirect";
            _authStateService.CreateAuthState(Arg.Any<HttpRequest>()).Throws(new RedirectUrlException(exceptionMessage));

            PathString requestPath = new PathString(request);
            var routingRequest = CreateRoutingRequest(requestPath, false, false);
            var result = await _googleFitAuthorizationHandler.Evaluate(routingRequest);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<string>(((BadRequestObjectResult)result).Value);
            Assert.Equal(exceptionMessage, ((BadRequestObjectResult)result).Value);
        }

        [Fact]
        public async Task GivenRequestCanBeHandled_WhenRequestIsForAuthorization_RedirectsUser()
        {
            _authService.AuthUriRequest(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new AuthUriResponse { Uri = _fakeRedirectUri });

            var routingRequest = CreateRoutingRequest(googleFitAuthorizeRequest);
            var result = await _googleFitAuthorizationHandler.Evaluate(routingRequest);
            Assert.IsType<RedirectResult>(result);

            var actualResult = result as RedirectResult;
            AuthUriResponse authUriResponse = new AuthUriResponse { Uri = _fakeRedirectUri };
            var expectedRedirect = new RedirectResult(authUriResponse.Uri);
            Assert.Equal(expectedRedirect.Url, actualResult?.Url);
        }

        [Fact]
        public async Task GivenRequestTokenIsNotValidated_WhenRequestIsForAuthorization_ReturnsUnauthorizedResult()
        {
            _authService.AuthUriRequest(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new AuthUriResponse { Uri = _fakeRedirectUri });
            _tokenValidationService.ValidateToken(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>()).Returns(false);

            var routingRequest = CreateRoutingRequest(googleFitAuthorizeRequest);
            var result = await _googleFitAuthorizationHandler.Evaluate(routingRequest);
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GivenRequestHandledAndUserExists_WhenRequestIsCallback_ReturnsRedirectResultWithExpectedUrl()
        {
            string redirectUrl = "http://localhost";
            _usersService.ProcessAuthorizationCallback(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<string>(redirectUrl));

            var routingRequest = CreateRoutingRequest(googleFitCallbackRequest);
            var result = await _googleFitAuthorizationHandler.Evaluate(routingRequest);
            Assert.IsType<RedirectResult>(result);

            var actualResult = result as RedirectResult;
            var expectedResult = new RedirectResult(redirectUrl);
            Assert.Equal(expectedResult.Url, actualResult?.Url);
        }

        [Fact]
        public async Task GivenRequestHandledAndExceptionIsThrown_WhenRequestIsCallback_ReturnsNotFoundResult()
        {
            _usersService.ProcessAuthorizationCallback(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new Exception("exception"));

            var routingRequest = CreateRoutingRequest(googleFitCallbackRequest);
            var result = await _googleFitAuthorizationHandler.Evaluate(routingRequest);
            Assert.IsType<NotFoundObjectResult>(result);

            var actualResult = result as NotFoundObjectResult;
            var expectedResult = new NotFoundObjectResult("exception");
            Assert.Equal(expectedResult.Value, actualResult?.Value);
        }

        [Fact]
        public async Task GivenRequestHandledAndExceptionIsThrown_WhenRequestIsRevoke_ReturnsInternalServerError()
        {
            string exceptionMessage = "process dataset exception";
            var exception = new Exception(exceptionMessage);
            _usersService.RevokeAccess(Arg.Any<AuthState>(), Arg.Any<CancellationToken>()).Throws(exception);

            var routingRequest = CreateRoutingRequest(googleFitRevokeRequest);
            var result = await _googleFitAuthorizationHandler.Evaluate(routingRequest);
            Assert.IsType<ObjectResult>(result);

            var actualResult = result as ObjectResult;
            var expectedResult = new ObjectResult("An unexpected error occurred while attempting to revoke data access.");
            Assert.Equal(expectedResult.Value, actualResult?.Value);
            Assert.Equal(StatusCodes.Status500InternalServerError, actualResult.StatusCode);

            _logger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                Arg.Is<Exception>(exc => exc == exception),
                Arg.Is<string>(msg => msg == exceptionMessage));
        }

        [Fact]
        public async Task GivenRequestHandledAndAllConditionsMet_WhenRequestIsRevoke_ReturnsOkObjectResult()
        {
            _usersService.RevokeAccess(Arg.Any<AuthState>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

            var routingRequest = CreateRoutingRequest(googleFitRevokeRequest);
            var result = await _googleFitAuthorizationHandler.Evaluate(routingRequest);
            Assert.IsType<OkObjectResult>(result);

            var actualResult = result as OkObjectResult;
            var expectedResult = new OkObjectResult("Access revoked successfully.");
            Assert.Equal(expectedResult.Value, actualResult?.Value);
        }

        [Fact]
        public async Task GivenRequestHandledAndTokenIsInvalid_WhenRequestIsRevoke_ReturnsUnauthorizedResult()
        {
            _tokenValidationService.ValidateToken(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>()).Returns(false);

            var routingRequest = CreateRoutingRequest(googleFitRevokeRequest);
            var result = await _googleFitAuthorizationHandler.Evaluate(routingRequest);
            Assert.IsType<UnauthorizedResult>(result);
        }

        private RoutingRequest CreateRoutingRequest(PathString pathString, bool includePatientId = true, bool includeSystem = true)
        {
            var httpRequest = Substitute.For<HttpRequest>();
            httpRequest.Path = pathString;

            if (pathString == googleFitAuthorizeRequest || pathString == googleFitRevokeRequest)
            {
                if (includePatientId)
                {
                    httpRequest.Query[Constants.ExternalIdQueryParameter].Returns(new StringValues(Data.ExternalPatientId));
                }

                if (includeSystem)
                {
                    httpRequest.Query[Constants.ExternalSystemQueryParameter].Returns(new StringValues(Data.ExternalSystem));
                }
            }
            else if (pathString == googleFitCallbackRequest)
            {
                httpRequest.Query["code"].Returns(new StringValues("access code"));
                httpRequest.Query["state"].Returns(new StringValues(Data.AuthorizationState));
            }

            var context = new ExecutionContext();

            return new RoutingRequest(httpRequest, context, CancellationToken.None);
        }
    }
}
