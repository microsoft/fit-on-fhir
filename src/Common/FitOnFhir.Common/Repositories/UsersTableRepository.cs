// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Health.FitOnFhir.Common.Models;
using Microsoft.Health.FitOnFhir.Common.Providers;

namespace Microsoft.Health.FitOnFhir.Common.Repositories
{
    public class UsersTableRepository : TableRepository<User>, IUsersTableRepository
    {
        public UsersTableRepository(ITableClientProvider tableClientProvider, ILogger<UsersTableRepository> logger)
            : base(tableClientProvider, logger)
        {
            PartitionKey = Constants.UsersPartitionKey;
        }
    }
}
