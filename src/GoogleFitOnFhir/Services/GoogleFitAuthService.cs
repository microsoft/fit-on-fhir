// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using GoogleFitOnFhir.Clients.GoogleFit;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;
using Microsoft.Extensions.Logging;

namespace GoogleFitOnFhir.Services
{
    public class GoogleFitAuthService : IGoogleFitAuthService
    {
        private readonly ILogger<GoogleFitAuthService> _logger;
        private readonly GoogleFitClientContext _clientContext;
        private readonly IGoogleFitAuthUriRequest _googleFitAuthUriRequest;
        private readonly IGoogleFitAuthTokensRequest _googleFitAuthTokensRequest;
        private readonly IGoogleFitRefreshTokenRequest _googleFitRefreshTokensRequest;

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleFitAuthService"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> for this service.</param>
        /// <param name="clientContext">The <see cref="GoogleFitClientContext"/> to be used by this service.</param>
        /// <param name="googleFitAuthUriRequest">The <see cref="IGoogleFitAuthUriRequest"/> used to initiate authorization.</param>
        /// <param name="googleFitAuthTokensRequest">The <see cref="IGoogleFitAuthTokensRequest"/> used to retrieve the auth token.</param>
        /// <param name="googleFitRefreshTokenRequest">The <see cref="IGoogleFitRefreshTokenRequest"/> used to refresh the access token.</param>
        public GoogleFitAuthService(
            ILogger<GoogleFitAuthService> logger,
            GoogleFitClientContext clientContext,
            IGoogleFitAuthUriRequest googleFitAuthUriRequest,
            IGoogleFitAuthTokensRequest googleFitAuthTokensRequest,
            IGoogleFitRefreshTokenRequest googleFitRefreshTokenRequest)
        {
            _logger = logger;
            _clientContext = EnsureArg.IsNotNull(clientContext);
            _googleFitAuthUriRequest = EnsureArg.IsNotNull(googleFitAuthUriRequest);
            _googleFitAuthTokensRequest = EnsureArg.IsNotNull(googleFitAuthTokensRequest);
            _googleFitRefreshTokensRequest = EnsureArg.IsNotNull(googleFitRefreshTokenRequest);
        }

        /// <inheritdoc/>
        public Task<AuthUriResponse> AuthUriRequest(CancellationToken cancellationToken)
        {
            return _googleFitAuthUriRequest.ExecuteAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<AuthTokensResponse> AuthTokensRequest(string authCode, CancellationToken cancellationToken)
        {
            _googleFitAuthTokensRequest.SetAuthCode(authCode);
            return _googleFitAuthTokensRequest.ExecuteAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<AuthTokensResponse> RefreshTokensRequest(string refreshToken, CancellationToken cancellationToken)
        {
            _googleFitRefreshTokensRequest.SetRefreshToken(refreshToken);
            return _googleFitRefreshTokensRequest.ExecuteAsync(cancellationToken);
        }
    }
}
