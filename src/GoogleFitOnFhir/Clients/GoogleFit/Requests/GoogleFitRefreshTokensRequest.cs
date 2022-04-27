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
    public class GoogleFitRefreshTokensRequest : IGoogleFitRefreshTokenRequest
    {
        private string _refreshToken;
        private IAuthorizationCodeFlow _authFlow;

        public GoogleFitRefreshTokensRequest()
        {
        }

        public void SetRefreshTokenAndAuthFlow(string refreshToken, IAuthorizationCodeFlow authFlow)
        {
            _refreshToken = refreshToken;
            _authFlow = authFlow;
        }

        public async Task<AuthTokensResponse> ExecuteAsync(CancellationToken cancellationToken)
        {
            TokenResponse tokenResponse = await _authFlow.RefreshTokenAsync("me", _refreshToken, cancellationToken);

            AuthTokensResponse.TryParse(tokenResponse, out AuthTokensResponse response);
            return response;
        }
    }
}