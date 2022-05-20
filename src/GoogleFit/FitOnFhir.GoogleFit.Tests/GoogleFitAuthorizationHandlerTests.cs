// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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
        private readonly PathString emptyGoogleFitRequest = "/api/googlefit/";

        private static string _fakeRedirectUri = "http://localhost";

        private readonly IGoogleFitAuthService _authService;
        private readonly IUsersService _usersService;
        private readonly ILogger<GoogleFitAuthorizationHandler> _logger;

        public GoogleFitAuthorizationHandlerTests()
        {
            _authService = Substitute.For<IGoogleFitAuthService>();
            _usersService = Substitute.For<IUsersService>();
            _logger = NullLogger<GoogleFitAuthorizationHandler>.Instance;

            _googleFitAuthorizationHandler = new GoogleFitAuthorizationHandler(_authService, _usersService, _logger);
        }

        [Fact]
        public void GivenRequestCannotBeHandled_WhenEvaluateIsCalled_NullIsReturned()
        {
            var routingRequest = CreateRoutingRequest(emptyGoogleFitRequest);
            var result = _googleFitAuthorizationHandler.Evaluate(routingRequest);

            Assert.Null(result);
        }

        [Fact]
        public async Task GivenRequestHandled_WhenRequestIsForAuthorization_RedirectsUser()
        {
            _authService.AuthUriRequest(Arg.Any<CancellationToken>()).Returns(new AuthUriResponse { Uri = _fakeRedirectUri });

            var routingRequest = CreateRoutingRequest(googleFitAuthorizeRequest);
            var result = await _googleFitAuthorizationHandler.Evaluate(routingRequest);
            Assert.IsType<RedirectResult>(result);

            var actualResult = result as RedirectResult;
            AuthUriResponse authUriResponse = new AuthUriResponse { Uri = _fakeRedirectUri };
            var expectedRedirect = new RedirectResult(authUriResponse.Uri);
            Assert.Equal(expectedRedirect.Url, actualResult?.Url);
        }

        [Fact]
        public async Task GivenRequestHandledAndUserExists_WhenRequestIsCallback_ReturnsOkResult()
        {
            _usersService.Initiate(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new User("test user", GoogleFitConstants.GoogleFitPlatformName));

            var routingRequest = CreateRoutingRequest(googleFitCallbackRequest);
            var result = await _googleFitAuthorizationHandler.Evaluate(routingRequest);
            Assert.IsType<OkObjectResult>(result);

            var actualResult = result as OkObjectResult;
            var expectedResult = new OkObjectResult("auth flow success");
            Assert.Equal(expectedResult.Value, actualResult?.Value);
        }

        [Fact]
        public async Task GivenRequestHandledAndExceptionIsThrown_WhenRequestIsCallback_ReturnsNotFoundResult()
        {
            _usersService.Initiate(Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new Exception("exception"));

            var routingRequest = CreateRoutingRequest(googleFitCallbackRequest);
            var result = await _googleFitAuthorizationHandler.Evaluate(routingRequest);
            Assert.IsType<NotFoundObjectResult>(result);

            var actualResult = result as NotFoundObjectResult;
            var expectedResult = new NotFoundObjectResult("exception");
            Assert.Equal(expectedResult.Value, actualResult?.Value);
        }

        private RoutingRequest CreateRoutingRequest(PathString pathString)
        {
            var httpRequest = Substitute.For<HttpRequest>();
            httpRequest.Path = pathString;
            httpRequest.Query["code"].Returns(new StringValues("access code"));
            var context = new ExecutionContext();

            return new RoutingRequest() { HttpRequest = httpRequest, Context = context, Token = CancellationToken.None };
        }
    }
}
