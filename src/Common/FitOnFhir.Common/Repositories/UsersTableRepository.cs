// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.Common.Config;
using FitOnFhir.Common.Models;
using FitOnFhir.Common.Persistence;
using Microsoft.Extensions.Logging;

namespace FitOnFhir.Common.Repositories
{
    public class UsersTableRepository : TableRepository<User>, IUsersTableRepository
    {
        public UsersTableRepository(AzureConfiguration azureConfiguration, ILogger<UsersTableRepository> logger)
            : base(azureConfiguration.StorageAccountConnectionString, logger)
        {
            PartitionKey = Constants.UsersPartitionKey;
        }
    }
}