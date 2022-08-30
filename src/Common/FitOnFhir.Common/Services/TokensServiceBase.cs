// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.FitOnFhir.Common.Exceptions;
using Microsoft.Health.FitOnFhir.Common.Interfaces;
using Microsoft.Health.FitOnFhir.Common.Models;
using Microsoft.Health.FitOnFhir.Common.Repositories;

namespace Microsoft.Health.FitOnFhir.Common.Services
{
    public abstract class TokensServiceBase<TTokenResponse> : ITokensService<TTokenResponse>
        where TTokenResponse : AuthTokenBase
    {
        private readonly IUsersKeyVaultRepository _usersKeyVaultRepository;
        private readonly ILogger _logger;

        protected TokensServiceBase(IUsersKeyVaultRepository usersKeyVaultRepository, ILogger logger)
        {
            _usersKeyVaultRepository = EnsureArg.IsNotNull(usersKeyVaultRepository, nameof(usersKeyVaultRepository));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        public async Task<TTokenResponse> RefreshToken(string userId, CancellationToken cancellationToken)
        {
            TTokenResponse tokenResponse = default;

            try
            {
                _logger.LogInformation("Get RefreshToken from KV for {0}", userId);
                var refreshToken = await _usersKeyVaultRepository.GetByName(userId, cancellationToken);

                _logger.LogInformation("Refreshing the RefreshToken");
                tokenResponse = await UpdateRefreshToken(refreshToken, cancellationToken);

                if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
                {
                    _logger.LogInformation("Updating refreshToken in KV for {0}", userId);
                    await _usersKeyVaultRepository.Upsert(userId, tokenResponse.RefreshToken, cancellationToken);
                }
                else
                {
                    _logger.LogInformation("RefreshToken is empty for {0}", userId);
                }
            }
            catch (Exception ex) when (ex is not TokenRefreshException)
            {
                _logger.LogError(ex, ex.Message);
            }

            return tokenResponse;
        }

        protected abstract Task<TTokenResponse> UpdateRefreshToken(string refreshToken, CancellationToken cancellationToken);
    }
}
