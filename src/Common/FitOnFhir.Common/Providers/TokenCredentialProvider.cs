// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Core;
using Azure.Identity;
using EnsureThat;
using Microsoft.Health.FitOnFhir.Common.Config;

namespace Microsoft.Health.FitOnFhir.Common.Providers
{
    public class TokenCredentialProvider : ITokenCredentialProvider
    {
        private readonly AzureConfiguration _azureConfiguration;

        public TokenCredentialProvider(AzureConfiguration azureConfiguration)
        {
            _azureConfiguration = EnsureArg.IsNotNull(azureConfiguration, nameof(azureConfiguration));
        }

        public TokenCredential GetTokenCredential()
        {
            if (string.IsNullOrWhiteSpace(_azureConfiguration?.FunctionPrincipalId))
            {
                return new DefaultAzureCredential();
            }

            return new ManagedIdentityCredential(_azureConfiguration.FunctionPrincipalId);
        }
    }
}
