// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using GoogleFitOnFhir.Clients.GoogleFit.Handlers;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;
using GoogleFitOnFhir.Common;
using GoogleFitOnFhir.Models;
using GoogleFitOnFhir.Services;
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

namespace GoogleFitOnFhir.UnitTests
{
    public class GoogleFitResponsibilityHandlerTests
    {
        private readonly IResponsibilityHandler<RoutingRequest, Task<IActionResult>> _googleFitHandler;

        private readonly PathString googleFitAuthorizeRequest = "/" + GoogleFitHandler.GoogleFitAuthorizeRequest;
        private readonly PathString googleFitCallbackRequest = "/" + GoogleFitHandler.GoogleFitCallbackRequest;
        private readonly PathString emptyGoogleFitRequest = "/api/googlefit/";

        private static string _fakeRedirectUri = "http://localhost";

        private readonly IAuthService _authService;
        private readonly IUsersService _usersService;
        private readonly ILogger<GoogleFitHandler> _logger;

        public GoogleFitResponsibilityHandlerTests()
        {
            _authService = Substitute.For<IAuthService>();
            _usersService = Substitute.For<IUsersService>();
            _logger = NullLogger<GoogleFitHandler>.Instance;

            _googleFitHandler = new GoogleFitHandler(_authService, _usersService, _logger);
        }

        [Fact]
        public void GivenRequestCannotBeHandled_WhenEvaluateIsCalled_NullIsReturned()
        {
            var routingRequest = CreateRoutingRequest(emptyGoogleFitRequest);
            var result = _googleFitHandler.Evaluate(routingRequest);

            Assert.Null(result);
        }

        [Fact]
        public async Task GivenRequestHandled_WhenRequestIsForAuthorization_RedirectsUser()
        {
            _authService.AuthUriRequest(Arg.Any<CancellationToken>()).Returns(new AuthUriResponse { Uri = _fakeRedirectUri });

            var routingRequest = CreateRoutingRequest(googleFitAuthorizeRequest);
            var result = await _googleFitHandler.Evaluate(routingRequest);
            Assert.IsType<RedirectResult>(result);

            var actualResult = result as RedirectResult;
            AuthUriResponse authUriResponse = new AuthUriResponse { Uri = _fakeRedirectUri };
            var expectedRedirect = new RedirectResult(authUriResponse.Uri);
            Assert.Equal(expectedRedirect.Url, actualResult?.Url);
        }

        [Fact]
        public async Task GivenRequestHandledAndUserExists_WhenRequestIsCallback_ReturnsOkResult()
        {
            _usersService.Initiate(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new User("test user"));

            var routingRequest = CreateRoutingRequest(googleFitCallbackRequest);
            var result = await _googleFitHandler.Evaluate(routingRequest);
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
            var result = await _googleFitHandler.Evaluate(routingRequest);
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
