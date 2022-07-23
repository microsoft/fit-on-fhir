// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.Common;
using FitOnFhir.Common.Interfaces;
using FitOnFhir.Common.Models;
using FitOnFhir.Common.Requests;
using FitOnFhir.GoogleFit.Client.Handlers;
using FitOnFhir.GoogleFit.Client.Responses;
using FitOnFhir.GoogleFit.Common;
using FitOnFhir.GoogleFit.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Common.Handler;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace FitOnFhir.GoogleFit.Tests
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
        private ITokenValidationService _tokenValidationService;
        private readonly ILogger<GoogleFitAuthorizationHandler> _logger;

        public GoogleFitAuthorizationHandlerTests()
        {
            _authService = Substitute.For<IGoogleFitAuthService>();
            _usersService = Substitute.For<IUsersService>();
            _tokenValidationService = Substitute.For<ITokenValidationService>();
            _logger = NullLogger<GoogleFitAuthorizationHandler>.Instance;

            _googleFitAuthorizationHandler = new GoogleFitAuthorizationHandler(
                _authService,
                _usersService,
                _tokenValidationService,
                _logger);

            _tokenValidationService.ValidateToken(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>()).Returns(true);
        }

        [Fact]
        public void GivenRequestCannotBeHandled_WhenEvaluateIsCalled_NullIsReturned()
        {
            var routingRequest = CreateRoutingRequest(emptyGoogleFitRequest);
            var result = _googleFitAuthorizationHandler.Evaluate(routingRequest);

            Assert.Null(result);
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public async Task GivenMissingRequiredQueryParameter_WhenEvaluateCalled_Returns400Response(bool includePatientId, bool includeSystem)
        {
            var routingRequest = CreateRoutingRequest(googleFitAuthorizeRequest, includePatientId, includeSystem);
            var result = await _googleFitAuthorizationHandler.Evaluate(routingRequest);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<string>(((BadRequestObjectResult)result).Value);
            Assert.Equal($"'{Constants.PatientIdQueryParameter}' and '{Constants.SystemQueryParameter}' are required query parameters.", ((BadRequestObjectResult)result).Value);
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
        public async Task GivenRequestHandledAndUserExists_WhenRequestIsCallback_ReturnsOkResult()
        {
            _usersService.ProcessAuthorizationCallback(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

            var routingRequest = CreateRoutingRequest(googleFitCallbackRequest);
            var result = await _googleFitAuthorizationHandler.Evaluate(routingRequest);
            Assert.IsType<OkObjectResult>(result);

            var actualResult = result as OkObjectResult;
            var expectedResult = new OkObjectResult("Authorization completed successfully.");
            Assert.Equal(expectedResult.Value, actualResult?.Value);
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

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        public async Task GivenRequestMissingPatientIdAndOrSystem_WhenRequestIsRevoke_ReturnsBadRequestObjectResult(bool includePatientId, bool includeSystem)
        {
            var routingRequest = CreateRoutingRequest(googleFitRevokeRequest, includePatientId, includeSystem);
            var result = await _googleFitAuthorizationHandler.Evaluate(routingRequest);
            Assert.IsType<BadRequestObjectResult>(result);

            var actualResult = result as BadRequestObjectResult;
            var expectedResult = new BadRequestObjectResult($"'{Constants.PatientIdQueryParameter}' and '{Constants.SystemQueryParameter}' are required query parameters.");
            Assert.Equal(expectedResult.Value, actualResult?.Value);
        }

        [Fact]
        public async Task GivenRequestHandledAndExceptionIsThrown_WhenRequestIsRevoke_ReturnsInternalServerError()
        {
            _usersService.RevokeAccess(Arg.Any<AuthState>(), Arg.Any<CancellationToken>()).Throws(new Exception("exception"));

            var routingRequest = CreateRoutingRequest(googleFitRevokeRequest);
            var result = await _googleFitAuthorizationHandler.Evaluate(routingRequest);
            Assert.IsType<ObjectResult>(result);

            var actualResult = result as ObjectResult;
            var expectedResult = new ObjectResult("An unexpected error occurred while attempting to revoke data access.");
            Assert.Equal(expectedResult.Value, actualResult?.Value);
            Assert.Equal(StatusCodes.Status500InternalServerError, actualResult.StatusCode);
        }

        private RoutingRequest CreateRoutingRequest(PathString pathString, bool includePatientId = true, bool includeSystem = true)
        {
            var httpRequest = Substitute.For<HttpRequest>();
            httpRequest.Path = pathString;

            if (pathString == googleFitAuthorizeRequest || pathString == googleFitRevokeRequest)
            {
                if (includePatientId)
                {
                    httpRequest.Query[Constants.PatientIdQueryParameter].Returns(new StringValues(Data.ExternalPatientId));
                }

                if (includeSystem)
                {
                    httpRequest.Query[Constants.SystemQueryParameter].Returns(new StringValues(Data.ExternalSystem));
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
