// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Common.Handler;
using Microsoft.Health.FitOnFhir.Common.Requests;
using Microsoft.Health.FitOnFhir.Common.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace Microsoft.Health.FitOnFhir.Common.Tests
{
    public class RoutingServiceTests
    {
        private readonly PathString _anyRequest = "/api/";
        private readonly PathString _validRequest = "/api/manufacturer/operation";

        private readonly ILogger<RoutingService> _logger;
        private readonly IResponsibilityHandler<RoutingRequest, Task<IActionResult>> _handler;
        private readonly IRoutingService _routingService;

        public RoutingServiceTests()
        {
            _handler = Substitute.For<IResponsibilityHandler<RoutingRequest, Task<IActionResult>>>();
            _logger = NullLogger<RoutingService>.Instance;
            _routingService = new RoutingService(_handler, _logger);
        }

        [Fact]
        public async Task GivenRequestCannotBeHandled_WhenRouteToIsCalled_NotFoundResultIsReturned()
        {
            _handler.Evaluate(Arg.Any<RoutingRequest>()).Returns(Task.FromResult<IActionResult>(new NotFoundResult()));

            var routingRequest = CreateRoutingRequest(_anyRequest);
            var result = await _routingService.RouteTo(routingRequest.HttpRequest, routingRequest.Context, routingRequest.Token);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GivenRequestHandledAndExceptionIsThrown_WhenRouteToIsCalled_ReturnsInternalServerErrorResponse()
        {
            string exceptionMessage = "failed to route request";
            var exception = new Exception(exceptionMessage);
            _handler.Evaluate(Arg.Any<RoutingRequest>()).Throws(exception);

            var routingRequest = CreateRoutingRequest(_anyRequest);
            var result = await _routingService.RouteTo(routingRequest.HttpRequest, routingRequest.Context, routingRequest.Token);

            Assert.IsType<ObjectResult>(result);

            var actualResult = result as ObjectResult;
            var expectedResult = new ObjectResult("Internal Server Error.");
            Assert.Equal(expectedResult.Value, actualResult?.Value);
            Assert.Equal(StatusCodes.Status500InternalServerError, actualResult.StatusCode);
        }

        [Fact]
        public async Task GivenValidRequestHandled_WhenRouteToIsCalled_ReturnsExpectedResult()
        {
            _handler.Evaluate(Arg.Is<RoutingRequest>(req => req.HttpRequest.Path == _validRequest)).Returns(Task.FromResult<IActionResult>(new OkResult()));

            var routingRequest = CreateRoutingRequest(_validRequest);
            var result = await _routingService.RouteTo(routingRequest.HttpRequest, routingRequest.Context, routingRequest.Token);

            Assert.IsType<OkResult>(result);
        }

        private RoutingRequest CreateRoutingRequest(PathString pathString)
        {
            var httpRequest = Substitute.For<HttpRequest>();
            httpRequest.Path = pathString;
            httpRequest.Query["code"].Returns(new StringValues("access code"));
            var context = new ExecutionContext();

            return new RoutingRequest(httpRequest, context, CancellationToken.None);
        }
    }
}
