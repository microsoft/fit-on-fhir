﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.FitOnFhir.Common;
using Microsoft.Health.FitOnFhir.Common.Exceptions;
using Microsoft.Health.FitOnFhir.Common.Interfaces;
using Microsoft.Health.FitOnFhir.Common.Models;
using Microsoft.Health.FitOnFhir.Common.Requests;
using Microsoft.Health.FitOnFhir.Common.Tests;
using Microsoft.Health.FitOnFhir.Common.Tests.Mocks;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Handlers;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Responses;
using Microsoft.Health.FitOnFhir.GoogleFit.Common;
using Microsoft.Health.FitOnFhir.GoogleFit.Services;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Tests
{
    public class GoogleFitAuthorizationHandlerTests : RequestHandlerBaseTests<RoutingRequest, Task<IActionResult>>
    {
        private readonly PathString googleFitAuthorizeRequest = "/" + GoogleFitConstants.GoogleFitAuthorizeRequest;
        private readonly PathString googleFitCallbackRequest = "/" + GoogleFitConstants.GoogleFitCallbackRequest;
        private readonly PathString googleFitRevokeRequest = "/" + GoogleFitConstants.GoogleFitRevokeAccessRequest;

        private static Uri _fakeRedirectUri = new Uri("http://localhost");

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

            RequestHandler = new GoogleFitAuthorizationHandler(
                _authService,
                _usersService,
                _tokenValidationService,
                _authStateService,
                _logger);

            _tokenValidationService.ValidateToken(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>()).Returns(true);
        }

        protected override RoutingRequest NonHandledRequest => CreateRoutingRequest("/unhandled route", false, false);

        [InlineData("/" + GoogleFitConstants.GoogleFitAuthorizeRequest)]
        [InlineData("/" + GoogleFitConstants.GoogleFitRevokeAccessRequest)]
        [Theory]
        public async Task GivenCreateAuthStateThrowsAuthStateException_WhenRequestIsNotCallback_ReturnsBadRequestObjectResult(string request)
        {
            string exceptionMsg = "AuthStateException occurred.";
            _authStateService.CreateAuthState(Arg.Any<HttpRequest>()).Throws(new AuthStateException(exceptionMsg));

            PathString requestPath = new PathString(request);
            var routingRequest = CreateRoutingRequest(requestPath, false, false);
            var result = await RequestHandler.Evaluate(routingRequest);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<string>(((BadRequestObjectResult)result).Value);
            Assert.Equal(exceptionMsg, ((BadRequestObjectResult)result).Value);
        }

        [InlineData("/" + GoogleFitConstants.GoogleFitAuthorizeRequest)]
        [InlineData("/" + GoogleFitConstants.GoogleFitRevokeAccessRequest)]
        [Theory]
        public async Task GivenCreateAuthStateThrowsException_WhenRequestIsNotCallback_ThrowsException(string request)
        {
            _authStateService.CreateAuthState(Arg.Any<HttpRequest>()).Throws(new Exception("failed to create auth state"));

            PathString requestPath = new PathString(request);
            var routingRequest = CreateRoutingRequest(requestPath, false, false);
            await Assert.ThrowsAsync<Exception>(() => RequestHandler.Evaluate(routingRequest));
        }

        [InlineData("/" + GoogleFitConstants.GoogleFitAuthorizeRequest)]
        [InlineData("/" + GoogleFitConstants.GoogleFitRevokeAccessRequest)]
        [Theory]
        public async Task GivenRequestTokenIsNotValidated_WhenRequestIsNotCallbaack_ReturnsUnauthorizedResult(string request)
        {
            if (request == googleFitAuthorizeRequest)
            {
                _authService.AuthUriRequest(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new AuthUriResponse { Uri = _fakeRedirectUri });
            }

            _tokenValidationService.ValidateToken(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>()).Returns(false);

            var routingRequest = CreateRoutingRequest(request);
            var result = await RequestHandler.Evaluate(routingRequest);
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GivenRequestCanBeHandled_WhenRequestIsForAuthorization_ReturnsCorrectAuthorizeResponseData()
        {
            DateTimeOffset now = DateTimeOffset.Now;
            AuthState authState = new AuthState("externalId", "externalSystem", now, _fakeRedirectUri);
            _authStateService.CreateAuthState(Arg.Any<HttpRequest>()).Returns(authState);

            _authService.AuthUriRequest(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new AuthUriResponse { Uri = _fakeRedirectUri });

            // send the routing request
            var routingRequest = CreateRoutingRequest(googleFitAuthorizeRequest);
            var result = await RequestHandler.Evaluate(routingRequest);

            Assert.IsType<JsonResult>(result);
            var actualJsonResult = result as JsonResult;

            AuthorizeResponseData expectedAuthResponseData = new AuthorizeResponseData(_fakeRedirectUri, now);
            var expectedJsonResult = new JsonResult(JsonConvert.SerializeObject(expectedAuthResponseData)) { StatusCode = (int?)HttpStatusCode.OK };

            // verify 200 OK response
            Assert.Equal(expectedJsonResult.StatusCode, actualJsonResult?.StatusCode);

            // verify the URL and ExpiresAt timestamp
            var actualAuthResponseDataResult = JsonConvert.DeserializeObject<AuthorizeResponseData>(actualJsonResult.Value.ToString());
            Assert.Equal(expectedAuthResponseData.AuthUrl, actualAuthResponseDataResult.AuthUrl);
            Assert.Equal(expectedAuthResponseData.ExpiresAt, actualAuthResponseDataResult.ExpiresAt);
        }

        [Fact]
        public async Task GivenRequestHandledAndUserExists_WhenRequestIsCallback_ReturnsRedirectResultWithExpectedUrl()
        {
            Uri redirectUrl = new Uri("http://testRedirectUrl");
            _usersService.ProcessAuthorizationCallback(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(redirectUrl));

            var routingRequest = CreateRoutingRequest(googleFitCallbackRequest);
            var result = await RequestHandler.Evaluate(routingRequest);
            Assert.IsType<RedirectResult>(result);

            var actualResult = result as RedirectResult;
            var expectedResult = new RedirectResult(redirectUrl.ToString());
            Assert.Equal(expectedResult.Url, actualResult?.Url);
        }

        [Fact]
        public async Task GivenRequestHandledAndExceptionIsThrown_WhenRequestIsCallback_ReturnsNotFoundResult()
        {
            _usersService.ProcessAuthorizationCallback(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new Exception("exception"));

            var routingRequest = CreateRoutingRequest(googleFitCallbackRequest);
            await Assert.ThrowsAsync<Exception>(() => RequestHandler.Evaluate(routingRequest));
        }

        [Fact]
        public async Task GivenRequestHandledAndExceptionIsThrown_WhenRequestIsRevoke_ThrowsException()
        {
            string exceptionMessage = "process dataset exception";
            var exception = new Exception(exceptionMessage);
            _usersService.RevokeAccess(Arg.Any<AuthState>(), Arg.Any<CancellationToken>()).Throws(exception);

            var routingRequest = CreateRoutingRequest(googleFitRevokeRequest);
            await Assert.ThrowsAsync<Exception>(() => RequestHandler.Evaluate(routingRequest));
        }

        [Fact]
        public async Task GivenRequestHandledAndAllConditionsMet_WhenRequestIsRevoke_ReturnsOkObjectResult()
        {
            _usersService.RevokeAccess(Arg.Any<AuthState>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

            var routingRequest = CreateRoutingRequest(googleFitRevokeRequest);
            var result = await RequestHandler.Evaluate(routingRequest);
            Assert.IsType<OkResult>(result);
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
