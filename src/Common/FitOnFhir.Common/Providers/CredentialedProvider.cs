// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Core;
using EnsureThat;

namespace Microsoft.Health.FitOnFhir.Common.Providers
{
    public abstract class CredentialedProvider
    {
        private readonly ITokenCredentialProvider _tokenCredentialProvider;

        protected CredentialedProvider(ITokenCredentialProvider tokenCredentialProvider)
        {
            _tokenCredentialProvider = EnsureArg.IsNotNull(tokenCredentialProvider, nameof(tokenCredentialProvider));
        }

        protected TokenCredential GetTokenCredential()
        {
            return _tokenCredentialProvider.GetTokenCredential();
        }
    }
}
