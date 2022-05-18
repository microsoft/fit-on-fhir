// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FitOnFhir.GoogleFit.Clients.GoogleFit;
using FitOnFhir.GoogleFit.Clients.GoogleFit.Responses;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2.Web;
using Microsoft.Extensions.Logging;

namespace FitOnFhir.GoogleFit.Services
{
    public class GoogleFitAuthService : IGoogleFitAuthService
    {
        private readonly ILogger<GoogleFitAuthService> _logger;
        private readonly GoogleFitClientContext _clientContext;
        private readonly GoogleAuthorizationCodeFlow _googleAuthorizationCodeFlow;

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleFitAuthService"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> for this service.</param>
        /// <param name="clientContext">The <see cref="GoogleFitClientContext"/> to be used by this service.</param>
        public GoogleFitAuthService(ILogger<GoogleFitAuthService> logger, GoogleFitClientContext clientContext)
        {
            _logger = EnsureArg.IsNotNull(logger);
            _clientContext = EnsureArg.IsNotNull(clientContext);
            _googleAuthorizationCodeFlow = new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = clientContext.ClientId,
                        ClientSecret = clientContext.ClientSecret,
                    },

                    Scopes = clientContext.DefaultScopes,
                });
        }

        /// <inheritdoc/>
        public async Task<AuthUriResponse> AuthUriRequest(CancellationToken cancellationToken)
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

        /// <inheritdoc/>
        public async Task<AuthTokensResponse> AuthTokensRequest(string authCode, CancellationToken cancellationToken)
        {
            TokenResponse tokenResponse = await _googleAuthorizationCodeFlow.ExchangeCodeForTokenAsync("me", authCode, _clientContext.CallbackUri, cancellationToken);

            AuthTokensResponse.TryParse(tokenResponse, out AuthTokensResponse response);
            return response;
        }

        /// <inheritdoc/>
        public async Task<AuthTokensResponse> RefreshTokensRequest(string refreshToken, CancellationToken cancellationToken)
        {
            TokenResponse tokenResponse = await _googleAuthorizationCodeFlow.RefreshTokenAsync("me", refreshToken, cancellationToken);

            AuthTokensResponse.TryParse(tokenResponse, out AuthTokensResponse response);
            return response;
        }
    }
}
