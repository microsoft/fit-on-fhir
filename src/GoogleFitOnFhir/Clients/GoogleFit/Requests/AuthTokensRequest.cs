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
        private readonly GoogleFitClientContext _clientContext;
        private readonly string _authCode;
        private readonly IAuthorizationCodeFlow _authFlow;

        public AuthTokensRequest(GoogleFitClientContext clientContext, string authCode, IAuthorizationCodeFlow authFlow)
        {
            _clientContext = clientContext;
            _authCode = authCode;
            _authFlow = authFlow;
        }

        public async Task<AuthTokensResponse> ExecuteAsync(CancellationToken cancellationToken)
        {
            TokenResponse tokenResponse = await _authFlow.ExchangeCodeForTokenAsync("me", _authCode, _clientContext.CallbackUri, cancellationToken);

            AuthTokensResponse.TryParse(tokenResponse, out AuthTokensResponse response);
            return response;
        }
    }
}