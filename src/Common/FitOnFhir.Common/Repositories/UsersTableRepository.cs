// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.Common.Models;
using FitOnFhir.Common.Persistence;
using Microsoft.Extensions.Logging;

namespace FitOnFhir.Common.Repositories
{
    public class UsersTableRepository : TableRepository<User>, IUsersTableRepository
    {
        public UsersTableRepository(StorageAccountContext storageAccountContext, ILogger<UsersTableRepository> logger)
            : base(storageAccountContext, logger)
        {
            PartitionKey = Constants.UsersPartitionKey;
        }
    }
}