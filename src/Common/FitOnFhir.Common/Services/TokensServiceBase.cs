// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using FitOnFhir.Common.Exceptions;
using FitOnFhir.Common.Models;
using FitOnFhir.Common.Repositories;
using Microsoft.Extensions.Logging;

namespace FitOnFhir.Common.Services
{
    public abstract class TokensServiceBase<TTokenResponse>
        where TTokenResponse : AuthTokenBase, new()
    {
        private readonly IUsersKeyVaultRepository _usersKeyVaultRepository;
        private readonly ILogger _logger;

        public TokensServiceBase()
        {
        }

        public TokensServiceBase(IUsersKeyVaultRepository usersKeyVaultRepository, ILogger logger)
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

        protected virtual Task<TTokenResponse> UpdateRefreshToken(string refreshToken, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
