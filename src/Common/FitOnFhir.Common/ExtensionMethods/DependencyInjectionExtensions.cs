// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Common.Handler;

namespace Microsoft.Health.FitOnFhir.Common.ExtensionMethods
{
    /// <summary>
    /// Provides a set of extension methods for dependency injection methods for the Identity function.
    /// </summary>
    public static class DependencyInjectionExtensions
    {
        /// <summary>
        /// Creates a chain of <see cref="IResponsibilityHandler{TRequest,TResult}"/> handlers, and registers the first one as the base
        /// operation handler.  This must be called after all handlers have already been registered with the <see cref="IServiceProvider"/>.
        /// Will throw an <see cref="ArgumentException"/> if <paramref name="handlerTypes"/> is empty, or does not contain elements of type IResponsibilityHandler&lt;RoutingRequest, Task&lt;IActionResult&gt;&gt;
        /// </summary>
        /// <typeparam name="TRequest">Type representing the responsibility handler's input request.</typeparam>
        /// <typeparam name="TResult">Type representing the responsibility handler's output.</typeparam>
        /// <param name="serviceProvider">The service provider with the registered handlers.</param>
        /// <param name="handlerTypes">The list of handlers to form a chain from.</param>
        public static IResponsibilityHandler<TRequest, TResult> CreateOrderedHandlerChain<TRequest, TResult>(
            this IServiceProvider serviceProvider,
            params Type[] handlerTypes)
            where TResult : class
        {
            if (handlerTypes.Any() && handlerTypes.All(handler => handler.GetInterfaces().Contains(typeof(IResponsibilityHandler<TRequest, TResult>))))
            {
                IResponsibilityHandler<TRequest, TResult> previousHandler = null;

                // Loop through the handlerTypes retrieve the instance and chain them together.
                foreach (Type handlerType in handlerTypes)
                {
                    IResponsibilityHandler<TRequest, TResult> handler = serviceProvider.GetRequiredService(handlerType) as IResponsibilityHandler<TRequest, TResult>;
                    if (previousHandler != null)
                    {
                        previousHandler.Chain(handler);
                    }

                    previousHandler = handler;
                }

                // Return the first handler. This will register the first handler in the chain with the Service Provider
                return serviceProvider.GetRequiredService(handlerTypes[0]) as IResponsibilityHandler<TRequest, TResult>;
            }
            else
            {
                throw new ArgumentException("handlers empty or wrong type");
            }
        }
    }
}
