// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;
using GoogleFitOnFhir.Services;

namespace GoogleFitOnFhir.Clients.GoogleFit.Requests
{
    public class GoogleFitAuthTokensRequest : IGoogleFitAuthTokensRequest
    {
        private GoogleFitClientContext _clientContext;
        private string _authCode;
        private IAuthorizationCodeFlow _authFlow;

        public GoogleFitAuthTokensRequest(GoogleFitClientContext clientContext)
        {
            _clientContext = clientContext;
        }

        public void SetAuthCodeAndFlow(string authCode, IAuthorizationCodeFlow authFlow)
        {
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