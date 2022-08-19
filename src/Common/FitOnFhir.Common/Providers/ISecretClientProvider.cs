// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Security.KeyVault.Secrets;

namespace Microsoft.Health.FitOnFhir.Common.Providers
{
    public interface ISecretClientProvider
    {
        /// <summary>
        /// Returns a instance of SecretClient.
        /// </summary>
        /// <returns><see cref="SecretClient"/></returns>
        public SecretClient GetSecretClient();
    }
}
