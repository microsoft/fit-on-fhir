// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.FitOnFhir.Common.Repositories
{
    public interface IUsersKeyVaultRepository
    {
        Task Upsert(string secretName, string value, CancellationToken cancellationToken);

        Task<string> GetByName(string secretName, CancellationToken cancellationToken);
    }
}
