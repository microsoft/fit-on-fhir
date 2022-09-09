// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Microsoft.Health.FitOnFhir.Common.Config;
using Microsoft.Health.FitOnFhir.Common.Models;

namespace Microsoft.Health.FitOnFhir.Common.Repositories
{
    public class UsersTableRepository : TableRepository<User>, IUsersTableRepository
    {
        public UsersTableRepository(AzureConfiguration azureConfiguration, TableClient tableClient, ILogger<UsersTableRepository> logger)
            : base(azureConfiguration?.StorageAccountConnectionString, tableClient, logger)
        {
            PartitionKey = Constants.UsersPartitionKey;
        }
    }
}
