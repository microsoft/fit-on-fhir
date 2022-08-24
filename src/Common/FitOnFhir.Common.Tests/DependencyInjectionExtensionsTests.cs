// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Common.Handler;
using Microsoft.Health.FitOnFhir.Common.ExtensionMethods;
using Microsoft.Health.FitOnFhir.Common.Requests;
using Microsoft.Health.FitOnFhir.Common.Tests.Mocks;
using NSubstitute;
using Xunit;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace Microsoft.Health.FitOnFhir.Common.Tests
{
    public class DependencyInjectionExtensionsTests
    {
        private readonly IServiceProvider _serviceProvider;

        public DependencyInjectionExtensionsTests()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<MockFirstResponsibilityHandler>();
            serviceCollection.AddSingleton<MockNextResponsibilityHandler>();
            serviceCollection.AddSingleton<MockLastResponsibilityHandler>();
            serviceCollection.AddSingleton<MockMismatchedInterfaceResponsibilityHandler>();
            _serviceProvider = serviceCollection.BuildServiceProvider(true);
        }

        [InlineData("/firstHandlerPlatform")]
        [InlineData("/nextHandlerPlatform")]
        [InlineData("/lastHandlerPlatform")]
        [Theory]
        public async Task GivenHandlersAreRegistered_WhenCreateOrderedHandlerChainIsCalled_HandlerIsAddedToChain(string route)
        {
            Type expectedResult;

            switch (route)
            {
                case "/firstHandlerPlatform":
                    expectedResult = typeof(OkResult);
                    break;
                case "/nextHandlerPlatform":
                    expectedResult = typeof(UnauthorizedResult);
                    break;
                case "/lastHandlerPlatform":
                    expectedResult = typeof(BadRequestResult);
                    break;
                default: throw new ArgumentException("invalid route passed to test");
            }

            var service =
                _serviceProvider.CreateOrderedHandlerChain<RoutingRequest, Task<IActionResult>>(
                    typeof(MockFirstResponsibilityHandler),
                    typeof(MockNextResponsibilityHandler),
                    typeof(MockLastResponsibilityHandler));

            Assert.IsAssignableFrom<IResponsibilityHandler<RoutingRequest, Task<IActionResult>>>(service);

            var routingRequest = CreateRoutingRequest(route);
            var result = await service.Evaluate(routingRequest);
            Assert.True(expectedResult == result.GetType());
        }

        [Fact]
        public void GivenNoHandlersAreReferenced_WhenCreateOrderedHandlerChainIsCalled_ArgumentExceptionIsThrown()
        {
            Assert.Throws<ArgumentException>(() => _serviceProvider.CreateOrderedHandlerChain<RoutingRequest, Task<IActionResult>>());
        }

        [Fact]
        public void GivenHandlerOfWrongTypeIsReferenced_WhenCreateOrderedHandlerChainIsCalled_ArgumentExceptionIsThrown()
        {
            Assert.Throws<ArgumentException>(() => _serviceProvider.CreateOrderedHandlerChain<RoutingRequest, Task<IActionResult>>(typeof(MockMismatchedInterfaceResponsibilityHandler), typeof(MockFirstResponsibilityHandler)));
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
