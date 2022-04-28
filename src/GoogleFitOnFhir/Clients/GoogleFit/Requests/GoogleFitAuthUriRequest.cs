// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Web;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;
using GoogleFitOnFhir.Services;

namespace GoogleFitOnFhir.Clients.GoogleFit.Requests
{
    public class GoogleFitAuthUriRequest : IGoogleFitAuthUriRequest
    {
        private readonly GoogleFitClientContext _clientContext;
        private readonly GoogleAuthorizationCodeFlow _googleAuthorizationCodeFlow;

        public GoogleFitAuthUriRequest(GoogleFitClientContext clientContext, GoogleAuthorizationCodeFlow googleAuthorizationCodeFlow)
        {
            _clientContext = clientContext;
            _googleAuthorizationCodeFlow = googleAuthorizationCodeFlow;
        }

        public async Task<AuthUriResponse> ExecuteAsync(CancellationToken cancellationToken)
        {
            var request = new AuthorizationCodeWebApp(
                _googleAuthorizationCodeFlow,
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