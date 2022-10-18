// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Security.KeyVault.Secrets;
using EnsureThat;
using Microsoft.Health.FitOnFhir.Common.Config;

namespace Microsoft.Health.FitOnFhir.Common.Providers
{
    public class SecretClientProvider : CredentialedProvider, ISecretClientProvider
    {
        private readonly Uri _vaultUri;

        public SecretClientProvider(AzureConfiguration configuration, ITokenCredentialProvider tokenCredentialProvider)
            : base(tokenCredentialProvider)
        {
            _vaultUri = EnsureArg.IsNotNull(configuration?.VaultUri, nameof(configuration.VaultUri));
        }

        public SecretClient GetSecretClient()
        {
            return new SecretClient(_vaultUri, GetTokenCredential());
        }
    }
}
