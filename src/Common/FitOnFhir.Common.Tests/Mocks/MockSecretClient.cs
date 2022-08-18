// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Azure.Security.KeyVault.Secrets;

namespace Microsoft.Health.FitOnFhir.Common.Tests.Mocks
{
    public abstract class MockSecretClient : SecretClient
    {
        public override Task<Response<KeyVaultSecret>> GetSecretAsync(string name, string version = null, CancellationToken cancellationToken = default)
        {
            return null;
        }

        public override Task<Response<KeyVaultSecret>> SetSecretAsync(KeyVaultSecret secret, CancellationToken cancellationToken = default)
        {
            return null;
        }

        public override Task<Response<DeletedSecret>> GetDeletedSecretAsync(string name, CancellationToken cancellationToken = default)
        {
            return null;
        }

        public override Task<RecoverDeletedSecretOperation> StartRecoverDeletedSecretAsync(string name, CancellationToken cancellationToken = default)
        {
            return null;
        }
    }
}
