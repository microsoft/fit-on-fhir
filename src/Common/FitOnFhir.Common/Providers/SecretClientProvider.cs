// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using EnsureThat;
using Microsoft.Health.FitOnFhir.Common.Config;

namespace Microsoft.Health.FitOnFhir.Common.Providers
{
    public class SecretClientProvider : ISecretClientProvider
    {
        private readonly AzureConfiguration _configuration;

        public SecretClientProvider(AzureConfiguration configuration)
        {
            _configuration = EnsureArg.IsNotNull(configuration, nameof(configuration));
        }

        public SecretClient GetSecretClient()
        {
            return new SecretClient(_configuration.UsersKeyVaultUri, new DefaultAzureCredential());
        }
    }
}
