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
using Microsoft.Health.FitOnFhir.Common.Handlers;
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
        private const string _okResultHandlerRoute = "okResultHandler";
        private const string _unauthorizedResultHandlerRoute = "unauthorizedResultHandler";
        private const string _badRequestResultHandlerRoute = "badRequestResultHandler";
        private const string _signOutResultHandlerRoute = "signOutResultHandler";
        private const string _unhandledRoute = "unhandledRoute";

        public DependencyInjectionExtensionsTests()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(new MockResponsibilityHandler<OkResult>(_okResultHandlerRoute));
            serviceCollection.AddSingleton(new MockResponsibilityHandler<UnauthorizedResult>(_unauthorizedResultHandlerRoute));
            serviceCollection.AddSingleton(new MockResponsibilityHandler<BadRequestResult>(_badRequestResultHandlerRoute));
            serviceCollection.AddSingleton(new MockResponsibilityHandler<SignOutResult>(_signOutResultHandlerRoute));
            serviceCollection.AddSingleton<UnknownAuthorizationHandler>();
            serviceCollection.AddSingleton<MockMismatchedInterfaceResponsibilityHandler>();
            _serviceProvider = serviceCollection.BuildServiceProvider(true);
        }

        [InlineData(_okResultHandlerRoute, typeof(OkResult), 1)]
        [InlineData(_unauthorizedResultHandlerRoute, typeof(UnauthorizedResult), 2)]
        [InlineData(_badRequestResultHandlerRoute, typeof(BadRequestResult), 3)]
        [InlineData(_signOutResultHandlerRoute, typeof(SignOutResult), 4)]
        [InlineData(_unhandledRoute, typeof(NotFoundResult), 5)]
        [Theory]
        public async Task GivenHandlersAreRegistered_WhenCreateOrderedHandlerChainIsCalled_HandlerIsAddedToChain(string route, Type expectedType, int numHandlers)
        {
            var service = BuildHandlerChain(numHandlers);

            Assert.IsAssignableFrom<IResponsibilityHandler<RoutingRequest, Task<IActionResult>>>(service);

            var routingRequest = CreateRoutingRequest("/" + route);
            var result = await service.Evaluate(routingRequest);
            Assert.True(expectedType == result.GetType());
        }

        [Fact]
        public void GivenNotAllHandlersAreRegistered_WhenCreateOrderedHandlerChainIsCalled_InvalidOperationExceptionIsThrown()
        {
            Assert.Throws<InvalidOperationException>(() => _serviceProvider.CreateOrderedHandlerChain<RoutingRequest, Task<IActionResult>>(
                typeof(MockResponsibilityHandler<ViewResult>),
                typeof(MockResponsibilityHandler<SignOutResult>),
                typeof(UnknownAuthorizationHandler)));
        }

        [Fact]
        public void GivenNoHandlersAreReferenced_WhenCreateOrderedHandlerChainIsCalled_ArgumentExceptionIsThrown()
        {
            Assert.Throws<ArgumentException>(() => _serviceProvider.CreateOrderedHandlerChain<RoutingRequest, Task<IActionResult>>());
        }

        [Fact]
        public void GivenHandlerOfWrongTypeIsReferenced_WhenCreateOrderedHandlerChainIsCalled_ArgumentExceptionIsThrown()
        {
            Assert.Throws<ArgumentException>(() => _serviceProvider.CreateOrderedHandlerChain<RoutingRequest, Task<IActionResult>>(typeof(MockMismatchedInterfaceResponsibilityHandler), typeof(UnknownAuthorizationHandler)));
        }

        private RoutingRequest CreateRoutingRequest(PathString pathString)
        {
            var httpRequest = Substitute.For<HttpRequest>();
            httpRequest.Path = pathString;
            httpRequest.Query["code"].Returns(new StringValues("access code"));
            var context = new ExecutionContext();

            return new RoutingRequest(httpRequest, context, CancellationToken.None);
        }

        private IResponsibilityHandler<RoutingRequest, Task<IActionResult>> BuildHandlerChain(int numHandlersToChain)
        {
            Type[] registeredHandlerTypes =
                {
                    typeof(MockResponsibilityHandler<OkResult>),
                    typeof(MockResponsibilityHandler<UnauthorizedResult>),
                    typeof(MockResponsibilityHandler<BadRequestResult>),
                    typeof(MockResponsibilityHandler<SignOutResult>),
                    typeof(UnknownAuthorizationHandler),
                };

            Array.Resize(ref registeredHandlerTypes, numHandlersToChain);

            return _serviceProvider.CreateOrderedHandlerChain<RoutingRequest, Task<IActionResult>>(registeredHandlerTypes);
        }
    }
}
