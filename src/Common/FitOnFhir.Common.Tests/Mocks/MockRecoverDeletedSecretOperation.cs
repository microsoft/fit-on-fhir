// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Azure.Security.KeyVault.Secrets;

namespace Microsoft.Health.FitOnFhir.Common.Tests.Mocks
{
    public class MockRecoverDeletedSecretOperation : RecoverDeletedSecretOperation
    {
        public override ValueTask<Response<SecretProperties>> WaitForCompletionAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<Response<SecretProperties>>(null);
        }
    }
}
