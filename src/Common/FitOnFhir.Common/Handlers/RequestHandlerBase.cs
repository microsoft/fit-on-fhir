// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Health.Common.Handler;
using Microsoft.Health.FitOnFhir.Common.Requests;

namespace Microsoft.Health.FitOnFhir.Common.Handlers
{
    public abstract class RequestHandlerBase<TRequest, TResult> : IResponsibilityHandler<TRequest, TResult>
        where TRequest : RequestBase
        where TResult : class
    {
        /// <summary>
        /// The list of routes, or paths, in an <see cref="HttpRequest"/> that this handler is capable of processing.
        /// </summary>
        public abstract IEnumerable<string> HandledRoutes { get; }

        /// <summary>
        /// Checks to see that the Route in the request matches one of the declared routes for this handler.  If it
        /// does, then call EvaluateRequest to take the appropriate action.
        /// </summary>
        /// <param name="request">The incoming request for this handler to evaluate.</param>
        public TResult Evaluate(TRequest request)
        {
            if (!CanHandle(request))
            {
                return null;
            }

            return EvaluateRequest(request);
        }

        /// <summary>
        /// This method is the starting point for taking the appropriate action against the incoming request.
        /// </summary>
        /// <param name="request">The incoming request to be acted on.</param>
        public abstract TResult EvaluateRequest(TRequest request);

        /// <summary>
        /// Verifies that the Route in the request matches one declared in HandledRoutes.
        /// </summary>
        /// <param name="request">The incoming request which contains the route to verify.</param>
        /// <returns>true if the Route matches one of the declared HandledRoutes. null if none match, which passes evaluation to the next request handler in the chain for evaluation.</returns>
        private bool CanHandle(TRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request?.Route) && HandledRoutes.Any(x => string.Equals(x, request.Route, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return false;
        }
    }
}
