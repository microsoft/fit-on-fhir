// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using FitOnFhir.Common.Repositories;
using Microsoft.Extensions.Logging;

namespace FitOnFhir.Common.Services
{
    public abstract class TokensServiceBase<TTokenResponse>
        where TTokenResponse : class
    {
        private readonly IUsersKeyVaultRepository _usersKeyVaultRepository;
        private readonly ILogger _logger;

        public TokensServiceBase(IUsersKeyVaultRepository usersKeyVaultRepository, ILogger logger)
        {
            _usersKeyVaultRepository = EnsureArg.IsNotNull(usersKeyVaultRepository, nameof(usersKeyVaultRepository));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        protected async Task<string> RetrieveRefreshToken(string userId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Get RefreshToken from KV for {0}", userId);
            return await _usersKeyVaultRepository.GetByName(userId, cancellationToken);
        }

        protected async Task StoreRefreshToken(string userId, string refreshToken, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogInformation("Updating refreshToken in KV for {0}", userId);
                await _usersKeyVaultRepository.Upsert(userId, refreshToken, cancellationToken);
            }
            else
            {
                _logger.LogInformation("RefreshToken is empty for {0}", userId);
            }
        }

        public virtual Task<TTokenResponse> RefreshToken(string userId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
