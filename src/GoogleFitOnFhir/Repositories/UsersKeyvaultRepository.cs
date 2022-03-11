// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using GoogleFitOnFhir.Persistence;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Logging;

namespace GoogleFitOnFhir.Repositories
{
    public class UsersKeyvaultRepository : IUsersKeyvaultRepository
    {
        private readonly UsersKeyvaultContext _keyvaultContext;
        private readonly IKeyVaultClient _keyVaultClient;
        private readonly AzureServiceTokenProvider _tokenProvider;
        private readonly ILogger<UsersKeyvaultRepository> _logger;

        public UsersKeyvaultRepository(
            UsersKeyvaultContext keyvaultContext,
            ILogger<UsersKeyvaultRepository> logger)
        {
            _keyvaultContext = keyvaultContext;
            _logger = logger;
            _tokenProvider = new AzureServiceTokenProvider();
            _keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(_tokenProvider.KeyVaultTokenCallback));
        }

        public async Task Upsert(string secretName, string value)
        {
            await _keyVaultClient.SetSecretAsync(
                _keyvaultContext.Uri,
                secretName,
                value);
        }

        public async Task<string> GetByName(string secretName)
        {
            try
            {
                SecretBundle secret = await _keyVaultClient.GetSecretAsync(
                    _keyvaultContext.Uri,
                    secretName);
                return secret.Value;
            }
            catch (KeyVaultErrorException ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }
        }
    }
}