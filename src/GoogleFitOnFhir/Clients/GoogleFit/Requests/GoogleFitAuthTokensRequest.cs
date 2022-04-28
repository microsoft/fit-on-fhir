// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;
using GoogleFitOnFhir.Services;

namespace GoogleFitOnFhir.Clients.GoogleFit.Requests
{
    public class GoogleFitAuthTokensRequest : IGoogleFitAuthTokensRequest
    {
        private readonly GoogleFitClientContext _clientContext;
        private readonly GoogleAuthorizationCodeFlow _googleAuthorizationCodeFlow;
        private string _authCode;

        public GoogleFitAuthTokensRequest(GoogleFitClientContext clientContext, GoogleAuthorizationCodeFlow googleAuthorizationCodeFlow)
        {
            _clientContext = EnsureArg.IsNotNull(clientContext);
            _googleAuthorizationCodeFlow = EnsureArg.IsNotNull(googleAuthorizationCodeFlow);
        }

        public void SetAuthCode(string authCode) => _authCode = authCode;

        public async Task<AuthTokensResponse> ExecuteAsync(CancellationToken cancellationToken)
        {
            TokenResponse tokenResponse = await _googleAuthorizationCodeFlow.ExchangeCodeForTokenAsync("me", _authCode, _clientContext.CallbackUri, cancellationToken);

            AuthTokensResponse.TryParse(tokenResponse, out AuthTokensResponse response);
            return response;
        }
    }
}