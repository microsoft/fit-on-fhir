// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Health.Common.Handler;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace Microsoft.Health.FitOnFhir.Authorization.Services
{
    public interface IRoutingService
    {
        /// <summary>
        /// Passes along an <see cref="HttpRequest"/> to an <see cref="IResponsibilityHandler{TRequest,TResult}"/>, which
        /// can then evaluate it and have the appropriate platform specific handler take action.
        /// </summary>
        /// <param name="req">The <see cref="HttpRequest"/> to take action on.</param>
        /// <param name="context">The <see cref="Microsoft.Azure.WebJobs.ExecutionContext"/>.</param>
        /// <param name="cancellationToken">A cancellation token for graceful recovery.</param>
        /// <returns>The <see cref="IActionResult"/> of the action taken by the <see cref="IResponsibilityHandler{TRequest,TResult}"/>, if any.
        /// Returns NotFoundResult, if no handler exists that matches the request.</returns>
        public Task<IActionResult> RouteTo(HttpRequest req, ExecutionContext context, CancellationToken cancellationToken);
    }
}
