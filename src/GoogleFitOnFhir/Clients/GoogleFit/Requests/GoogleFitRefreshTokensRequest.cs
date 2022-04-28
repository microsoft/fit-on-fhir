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
    public class GoogleFitRefreshTokensRequest : IGoogleFitRefreshTokenRequest
    {
        private readonly GoogleAuthorizationCodeFlow _googleAuthorizationCodeFlow;
        private string _refreshToken;

        public GoogleFitRefreshTokensRequest(GoogleAuthorizationCodeFlow googleAuthorizationCodeFlow)
        {
            _googleAuthorizationCodeFlow = EnsureArg.IsNotNull(googleAuthorizationCodeFlow);
        }

        public void SetRefreshToken(string refreshToken) => _refreshToken = refreshToken;

        public async Task<AuthTokensResponse> ExecuteAsync(CancellationToken cancellationToken)
        {
            TokenResponse tokenResponse = await _googleAuthorizationCodeFlow.RefreshTokenAsync("me", _refreshToken, cancellationToken);

            AuthTokensResponse.TryParse(tokenResponse, out AuthTokensResponse response);
            return response;
        }
    }
}