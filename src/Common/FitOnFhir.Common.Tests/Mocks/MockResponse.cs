// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Azure.Security.KeyVault.Secrets;

namespace Microsoft.Health.FitOnFhir.Common.Tests.Mocks
{
    public class MockResponse : Response<DeletedSecret>
    {
        public override DeletedSecret Value => null;

        public override Response GetRawResponse()
        {
            return null;
        }
    }
}
