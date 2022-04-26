// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using GoogleFitOnFhir.Common;
using GoogleFitOnFhir.Identity;
using GoogleFitOnFhir.UnitTests.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GoogleFitOnFhir.UnitTests
{
    public class IdentityDependencyInjectionExtensionsTests
    {
        private readonly IServiceProvider _serviceProvider;

        public IdentityDependencyInjectionExtensionsTests()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<MockBaseResponsibilityHandler>();
            serviceCollection.AddSingleton<MockMismatchedInterfaceResponsibilityHandler>();
            serviceCollection.AddSingleton<UnknownOperationHandler>();
            _serviceProvider = serviceCollection.BuildServiceProvider(true);
        }

        [Fact]
        public void GivenHandlersAreRegistered_WhenCreateOrderedHandlerChainIsCalled_BaseHandlerIsReturned()
        {
            var service = _serviceProvider.CreateOrderedHandlerChain(typeof(MockBaseResponsibilityHandler), typeof(UnknownOperationHandler));
            Assert.IsType<MockBaseResponsibilityHandler>(service);
        }

        [Fact]
        public void GivenNoHandlersAreReferenced_WhenCreateOrderedHandlerChainIsCalled_ArgumentExceptionIsThrown()
        {
            Assert.Throws<ArgumentException>(() => _serviceProvider.CreateOrderedHandlerChain());
        }

        [Fact]
        public void GivenHandlerOfWrongTypeIsReferenced_WhenCreateOrderedHandlerChainIsCalled_ArgumentExceptionIsThrown()
        {
            Assert.Throws<ArgumentException>(() => _serviceProvider.CreateOrderedHandlerChain(typeof(MockMismatchedInterfaceResponsibilityHandler), typeof(UnknownOperationHandler)));
        }
    }
}
