// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using EnsureThat;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;
using GoogleFitOnFhir.Common;
using GoogleFitOnFhir.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Common.Handler;

namespace GoogleFitOnFhir.Clients.GoogleFit.Handlers
{
    public class GoogleFitAuthorizationHandler : IResponsibilityHandler<(IServiceScope scope, RoutingRequest request), Task<IActionResult>>
    {
        private const string GoogleFitAuthorizeRequest = "api/googlefit/authorize";

        private GoogleFitAuthorizationHandler()
        {
        }

        public static IResponsibilityHandler<(IServiceScope scope, RoutingRequest request), Task<IActionResult>> Instance { get; } = new GoogleFitAuthorizationHandler();

        public Task<IActionResult> Evaluate((IServiceScope scope, RoutingRequest request) operation)
        {
            var path = EnsureArg.IsNotNull(operation.request.HttpRequest.Path.Value?[1..]);

            if (path.StartsWith(GoogleFitAuthorizeRequest))
            {
                return Login(operation);
            }
            else
            {
                return null;
            }
        }

        private async Task<IActionResult> Login((IServiceScope scope, RoutingRequest request) operation)
        {
            var authService = operation.scope.ServiceProvider.GetRequiredService<IAuthService>();
            AuthUriResponse response = await authService.AuthUriRequest(operation.request.Token);
            return new RedirectResult(response.Uri);
        }
    }
}
