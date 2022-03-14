// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Web;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;

namespace GoogleFitOnFhir.Clients.GoogleFit.Requests
{
    public class AuthUriRequest
    {
        private readonly GoogleFitClientContext _clientContext;
        private readonly IAuthorizationCodeFlow _authFlow;

        public AuthUriRequest(GoogleFitClientContext clientContext, IAuthorizationCodeFlow authFlow)
        {
            _clientContext = clientContext;
            _authFlow = authFlow;
        }

        public async Task<AuthUriResponse> ExecuteAsync(CancellationToken cancellationToken)
        {
            var request = new AuthorizationCodeWebApp(
                _authFlow,
                _clientContext.CallbackUri,
                string.Empty);

            var result = await request.AuthorizeAsync("user", cancellationToken);
            if (result.Credential == null)
            {
                var response = new AuthUriResponse
                {
                    Uri = result.RedirectUri,
                };

                return response;
            }
            else
            {
                // Not sure when this would happen
                return null;
            }
        }
    }
}