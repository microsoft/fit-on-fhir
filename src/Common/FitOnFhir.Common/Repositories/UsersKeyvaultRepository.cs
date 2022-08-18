// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Security.KeyVault.Secrets;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.FitOnFhir.Common.Providers;

namespace Microsoft.Health.FitOnFhir.Common.Repositories
{
    public class UsersKeyVaultRepository : IUsersKeyVaultRepository
    {
        private readonly SecretClient _secretClient;
        private readonly ILogger<UsersKeyVaultRepository> _logger;

        public UsersKeyVaultRepository(
            ISecretClientProvider secretClientProvider,
            ILogger<UsersKeyVaultRepository> logger)
        {
            _secretClient = secretClientProvider.GetSecretClient();
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        public async Task Upsert(string secretName, string value, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(secretName, nameof(secretName));
            EnsureArg.IsNotNullOrWhiteSpace(value, nameof(value));

            // If purge protection is enabled, a secret with the same name needs to be recovered before it can be re-saved.
            if (await IsInDeletedStateAsync(secretName, cancellationToken))
            {
                RecoverDeletedSecretOperation operation = await _secretClient.StartRecoverDeletedSecretAsync(secretName, cancellationToken);
                await operation.WaitForCompletionAsync(cancellationToken);
            }

            var secret = new KeyVaultSecret(secretName, value);
            KeyVaultSecret createdSecret = await _secretClient.SetSecretAsync(secret, cancellationToken);
        }

        public async Task<string> GetByName(string secretName, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(secretName, nameof(secretName));

            KeyVaultSecret keyVaultSecret = await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
            return keyVaultSecret.Value;
        }

        private async Task<bool> IsInDeletedStateAsync(string secretName, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(secretName, nameof(secretName));

            try
            {
                var deletedSecret = await _secretClient.GetDeletedSecretAsync(secretName, cancellationToken);
                return deletedSecret != null;
            }
            catch
            {
                // An exception can be thrown if a deleted secret does not exist, or if purge protection is not enabled.
                return false;
            }
        }
    }
}