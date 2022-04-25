// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading.Tasks;
using GoogleFitOnFhir.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Common.Handler;

namespace GoogleFitOnFhir.Identity
{
    /// <summary>
    /// Provides a set of extension methods for dependency injection methods for the Identity function.
    /// </summary>
    public static class IdentityDependencyInjectionExtensions
    {
        /// <summary>
        /// Creates a chain of <see cref="IResponsibilityHandler{TRequest,TResult}"/> handlers, and registers the first one as the base
        /// operation handler.  This must be called after all handlers have already been registered with the <see cref="IServiceProvider"/>.
        /// Will throw an <see cref="ArgumentException"/> if <paramref name="handlerTypes"/> is empty, or does not contain elements of type IResponsibilityHandler&lt;RoutingRequest, Task&lt;IActionResult&gt;&gt;
        /// </summary>
        /// <param name="serviceProvider">The service provider with the registered handlers.</param>
        /// <param name="handlerTypes">The list of handlers to form a chain from.</param>
        public static IResponsibilityHandler<RoutingRequest, Task<IActionResult>> CreateOrderedHandlerChain(this IServiceProvider serviceProvider, params Type[] handlerTypes)
        {
            if (handlerTypes.Any())
            {
                IResponsibilityHandler<RoutingRequest, Task<IActionResult>> previousHandler = null;

                // Loop through the handlerTypes retrieve the instance and chain them together.
                foreach (Type handlerType in handlerTypes)
                {
                    IResponsibilityHandler<RoutingRequest, Task<IActionResult>> handler = serviceProvider.GetRequiredService(handlerType) as IResponsibilityHandler<RoutingRequest, Task<IActionResult>>;
                    if (previousHandler != null)
                    {
                        previousHandler.Chain(handler);
                    }

                    previousHandler = handler;
                }

                // Return the first handler. This will register the first handler in the chain with the Service Provider
                return serviceProvider.GetRequiredService(handlerTypes[0]) as IResponsibilityHandler<RoutingRequest, Task<IActionResult>>;
            }
            else
            {
                throw new ArgumentException("handlers empty");
            }
        }
    }
}
