// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using FitOnFhir.Common.Repositories;
using FitOnFhir.GoogleFit.Client.Responses;
using Microsoft.Extensions.Logging;

namespace FitOnFhir.GoogleFit.Services
{
    public class GoogleFitTokensService : IGoogleFitTokensService
    {
        private readonly IUsersKeyVaultRepository _usersKeyvaultRepository;
        private readonly IGoogleFitAuthService _googleFitAuthService;
        private readonly ILogger<GoogleFitTokensService> _logger;

        public GoogleFitTokensService(
            IUsersKeyVaultRepository usersKeyvaultRepository,
            IGoogleFitAuthService googleFitAuthService,
            ILogger<GoogleFitTokensService> logger)
        {
            _usersKeyvaultRepository = EnsureArg.IsNotNull(usersKeyvaultRepository, nameof(usersKeyvaultRepository));
            _googleFitAuthService = EnsureArg.IsNotNull(googleFitAuthService, nameof(googleFitAuthService));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        public async Task<AuthTokensResponse> RefreshToken(string googleFitId, CancellationToken cancellationToken)
        {
            string refreshToken;

            _logger.LogInformation("Get RefreshToken from KV for {0}", googleFitId);
            refreshToken = await _usersKeyvaultRepository.GetByName(googleFitId, cancellationToken);

            _logger.LogInformation("Refreshing the RefreshToken");
            AuthTokensResponse tokensResponse = await _googleFitAuthService.RefreshTokensRequest(refreshToken, cancellationToken);

            if (!string.IsNullOrEmpty(tokensResponse.RefreshToken))
            {
                _logger.LogInformation("Updating refreshToken in KV for {0}", googleFitId);
                await _usersKeyvaultRepository.Upsert(googleFitId, tokensResponse.RefreshToken, cancellationToken);
            }
            else
            {
                _logger.LogInformation("RefreshToken is empty for {0}", googleFitId);
            }

            return tokensResponse;
        }
    }
}
