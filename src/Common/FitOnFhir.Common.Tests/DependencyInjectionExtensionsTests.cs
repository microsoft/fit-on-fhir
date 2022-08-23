// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Common.Handler;
using Microsoft.Health.FitOnFhir.Common.ExtensionMethods;
using Microsoft.Health.FitOnFhir.Common.Handlers;
using Microsoft.Health.FitOnFhir.Common.Requests;
using Microsoft.Health.FitOnFhir.Common.Tests.Mocks;
using Xunit;

namespace Microsoft.Health.FitOnFhir.Common.Tests
{
    public class DependencyInjectionExtensionsTests
    {
        private readonly IServiceProvider _serviceProvider;

        public DependencyInjectionExtensionsTests()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<MockBaseResponsibilityHandler>();
            serviceCollection.AddSingleton<MockMismatchedInterfaceResponsibilityHandler>();
            serviceCollection.AddSingleton<UnknownAuthorizationHandler>();
            _serviceProvider = serviceCollection.BuildServiceProvider(true);
        }

        [Fact]
        public void GivenHandlersAreRegistered_WhenCreateOrderedHandlerChainIsCalled_BaseHandlerIsReturned()
        {
            var service = _serviceProvider.CreateOrderedHandlerChain<RoutingRequest, Task<IActionResult>>(typeof(MockBaseResponsibilityHandler), typeof(UnknownAuthorizationHandler));
            Assert.IsAssignableFrom<IResponsibilityHandler<RoutingRequest, Task<IActionResult>>>(service);
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
    }
}
