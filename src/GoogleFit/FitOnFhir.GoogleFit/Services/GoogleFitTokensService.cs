// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using FitOnFhir.Common.Exceptions;
using FitOnFhir.Common.Repositories;
using FitOnFhir.Common.Services;
using FitOnFhir.GoogleFit.Client.Responses;
using Microsoft.Extensions.Logging;

namespace FitOnFhir.GoogleFit.Services
{
    public class GoogleFitTokensService : TokensServiceBase<AuthTokensResponse>
    {
        private readonly IGoogleFitAuthService _googleFitAuthService;
        private readonly ILogger<GoogleFitTokensService> _logger;

        public GoogleFitTokensService(
            IGoogleFitAuthService googleFitAuthService,
            IUsersKeyVaultRepository usersKeyVaultRepository,
            ILogger<GoogleFitTokensService> logger)
        : base(usersKeyVaultRepository, logger)
        {
            _googleFitAuthService = EnsureArg.IsNotNull(googleFitAuthService, nameof(googleFitAuthService));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        public override async Task<AuthTokensResponse> RefreshToken(string googleFitId, CancellationToken cancellationToken)
        {
            AuthTokensResponse tokensResponse = null;

            try
            {
                var refreshToken = await RetrieveRefreshToken(googleFitId, cancellationToken);

                _logger.LogInformation("Refreshing the RefreshToken");
                tokensResponse = await _googleFitAuthService.RefreshTokensRequest(refreshToken, cancellationToken);

                await StoreRefreshToken(googleFitId, tokensResponse.RefreshToken, cancellationToken);
            }
            catch (Exception ex)
            {
                var tokenRefreshException = new TokenRefreshException(ex.Message);
                _logger.LogError(tokenRefreshException, tokenRefreshException.Message);
            }

            return tokensResponse;
        }
    }
}
