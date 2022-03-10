// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;

namespace GoogleFitOnFhir.Clients.GoogleFit.Requests
{
    public class AuthTokensRequest
    {
        private readonly ClientContext _clientContext;
        private readonly string _authCode;
        private readonly IAuthorizationCodeFlow _authFlow;

        public AuthTokensRequest(ClientContext clientContext, string authCode, IAuthorizationCodeFlow authFlow)
        {
            _clientContext = clientContext;
            _authCode = authCode;
            _authFlow = authFlow;
        }

        public async Task<AuthTokensResponse> ExecuteAsync()
        {
            TokenResponse tokenResponse = await _authFlow
                .ExchangeCodeForTokenAsync(
                    "me",
                    _authCode,
                    _clientContext.CallbackUri,
                    CancellationToken.None);

            if (tokenResponse != null && tokenResponse.RefreshToken != null)
            {
                AuthTokensResponse response = new AuthTokensResponse
                {
                    AccessToken = tokenResponse.AccessToken,
                    RefreshToken = tokenResponse.RefreshToken,
                };
                return response;
            }
            else
            {
                return null;
            }
        }
    }
}