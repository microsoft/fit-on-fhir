// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using EnsureThat;
using Microsoft.Extensions.Logging;

namespace FitOnFhir.GoogleFit.Repositories
{
    public class UsersKeyVaultRepository : IUsersKeyVaultRepository
    {
        private readonly SecretClient _secretClient;
        private readonly ILogger<UsersKeyVaultRepository> _logger;

        public UsersKeyVaultRepository(
            SecretClient secretClient,
            ILogger<UsersKeyVaultRepository> logger)
        {
            _secretClient = EnsureArg.IsNotNull(secretClient, nameof(secretClient));
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

            try
            {
                var secret = new KeyVaultSecret(secretName, value);
                KeyVaultSecret createdSecret = await _secretClient.SetSecretAsync(secret, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set secret: {credentialBundleName}.", secretName);
            }
        }

        public async Task<string> GetByName(string secretName, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(secretName, nameof(secretName));

            try
            {
                KeyVaultSecret keyVaultSecret = await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
                return keyVaultSecret.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
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