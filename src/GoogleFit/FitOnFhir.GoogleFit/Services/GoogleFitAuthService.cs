// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Config;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Responses;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Services
{
    public sealed class GoogleFitAuthService : IGoogleFitAuthService, IDisposable
    {
        private readonly GoogleFitAuthorizationConfiguration _authorizationConfiguration;
        private readonly GoogleAuthorizationCodeFlow _googleAuthorizationCodeFlow;

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleFitAuthService"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> for this service.</param>
        /// <param name="authorizationConfiguration">The <see cref="GoogleFitAuthorizationConfiguration"/> to be used by this service.</param>
        public GoogleFitAuthService(ILogger<GoogleFitAuthService> logger, GoogleFitAuthorizationConfiguration authorizationConfiguration)
        {
            _authorizationConfiguration = EnsureArg.IsNotNull(authorizationConfiguration);
            _googleAuthorizationCodeFlow = new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = authorizationConfiguration.ClientId,
                        ClientSecret = authorizationConfiguration.ClientSecret,
                    },

                    Scopes = authorizationConfiguration.AuthorizedScopes,
                });
        }

        /// <inheritdoc/>
        public async Task<AuthUriResponse> AuthUriRequest(string state, CancellationToken cancellationToken)
        {
            var request = new AuthorizationCodeWebApp(
                _googleAuthorizationCodeFlow,
                _authorizationConfiguration.CallbackUri.ToString(),
                state);

            var result = await request.AuthorizeAsync("user", cancellationToken);
            if (result.Credential == null)
            {
                var response = new AuthUriResponse
                {
                    Uri = new Uri(result.RedirectUri),
                };

                return response;
            }
            else
            {
                // Not sure when this would happen
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<AuthTokensResponse> AuthTokensRequest(string authCode, CancellationToken cancellationToken)
        {
            TokenResponse tokenResponse = await _googleAuthorizationCodeFlow.ExchangeCodeForTokenAsync("me", authCode, _authorizationConfiguration.CallbackUri.ToString(), cancellationToken);

            _ = AuthTokensResponse.TryParse(tokenResponse, out AuthTokensResponse response);

            return response;
        }

        /// <inheritdoc/>
        public async Task<AuthTokensResponse> RefreshTokensRequest(string refreshToken, CancellationToken cancellationToken)
        {
            TokenResponse tokenResponse = await _googleAuthorizationCodeFlow.RefreshTokenAsync("me", refreshToken, cancellationToken);

            _ = AuthTokensResponse.TryParse(tokenResponse, out AuthTokensResponse response);

            return response;
        }

        /// <inheritdoc/>
        public async Task RevokeTokenRequest(string accessToken, CancellationToken cancellationToken)
        {
            await _googleAuthorizationCodeFlow.RevokeTokenAsync("me", accessToken, cancellationToken);
        }

        public void Dispose()
        {
            _googleAuthorizationCodeFlow.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
