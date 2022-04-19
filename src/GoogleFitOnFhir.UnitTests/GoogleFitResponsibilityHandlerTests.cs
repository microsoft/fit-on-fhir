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

        private readonly PathString googleFitAuthorizeRequest = new PathString("/api/googlefit/authorize");
        private readonly PathString googleFitCallbackRequest = new PathString("/api/googlefit/callback");
        private readonly PathString emptyGoogleFitRequest = new PathString("/api/googlefit/");

        private static string _fakeRedirectUri = "http://localhost";

        private readonly IAuthService _authService;
        private readonly IUsersService _usersService;
        private readonly ILogger<GoogleFitHandler> _logger;

        public GoogleFitResponsibilityHandlerTests()
        {
            _authService = Substitute.For<IAuthService>();
            _usersService = Substitute.For<IUsersService>();
            _logger = Substitute.For<ILogger<GoogleFitHandler>>();

            _googleFitHandler = new GoogleFitHandler(_authService, _usersService, _logger);
        }

        [Fact]
        public void GoogleFitHandler_ReturnsNull_WhenRequestIsNotForAuthorization()
        {
            var routingRequest = CreateRoutingRequest(emptyGoogleFitRequest);
            var result = _googleFitHandler.Evaluate(routingRequest);

            Assert.Null(result);
        }

        [Fact]
        public async Task GoogleFitHandler_RedirectsUser_WhenRequestIsForAuthorization()
        {
            _authService.AuthUriRequest(Arg.Any<CancellationToken>()).Returns(new AuthUriResponse { Uri = _fakeRedirectUri });

            var routingRequest = CreateRoutingRequest(googleFitAuthorizeRequest);
            RedirectResult response = (RedirectResult)await _googleFitHandler.Evaluate(routingRequest);

            AuthUriResponse authUriResponse = new AuthUriResponse { Uri = _fakeRedirectUri };
            var expectedRedirect = new RedirectResult(authUriResponse.Uri);
            Assert.Equal(expectedRedirect.Url, response.Url);
        }

        [Fact]
        public async Task GoogleFitHandler_ReturnsOkResult_WhenRequestIsCallback_AndUserExists()
        {
            _usersService.Initiate(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new User("test user"));

            var routingRequest = CreateRoutingRequest(googleFitCallbackRequest);
            var result = (OkObjectResult)await _googleFitHandler.Evaluate(routingRequest);

            var expectedResult = new OkObjectResult("auth flow success");
            Assert.Equal(expectedResult.Value, result.Value);
        }

        [Fact]
        public async Task GoogleFitHandler_ReturnsNotFoundResult_WhenRequestIsCallback_AndExceptionIsThrown()
        {
            _usersService.Initiate(Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new Exception("exception"));

            var routingRequest = CreateRoutingRequest(googleFitCallbackRequest);
            var result = (NotFoundObjectResult)await _googleFitHandler.Evaluate(routingRequest);

            var expectedResult = new NotFoundObjectResult("exception");
            Assert.Equal(expectedResult.Value, result.Value);
        }

        private RoutingRequest CreateRoutingRequest(PathString pathString)
        {
            var httpRequest = Substitute.For<HttpRequest>();
            httpRequest.Path = pathString;
            httpRequest.Query["code"].Returns(new StringValues("access code"));
            var context = new ExecutionContext();
            var cancellationToken = new CancellationToken(false);
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, httpRequest.HttpContext.RequestAborted);

            return new RoutingRequest() { HttpRequest = httpRequest, Root = context.FunctionAppDirectory, Token = cancellationSource.Token };
        }
    }
}
