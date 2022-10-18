// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Microsoft.Health.FitOnFhir.Common.Models;
using Microsoft.Health.FitOnFhir.Common.Repositories;
using Microsoft.Health.FitOnFhir.Common.Tests.Mocks;
using NSubstitute;

namespace Microsoft.Health.FitOnFhir.Common.Tests
{
    public class UserTableRepositoryTests : TableRepositoryBaseTests<User>
    {
        private readonly MockLogger<UsersTableRepository> _usersTableRepositoryLogger;
        private readonly string _mergedUserId = "12345678-9101-1121-3141-516171819202";
        private readonly string _newUserId = "22345678-9101-1121-3141-516171819202";
        private readonly string _storedUserId = "32345678-9101-1121-3141-516171819202";

        private readonly DateTimeOffset _now =
            new DateTimeOffset(2004, 1, 12, 0, 0, 0, new TimeSpan(-5, 0, 0));

        public UserTableRepositoryTests()
        {
            _usersTableRepositoryLogger = Substitute.For<MockLogger<UsersTableRepository>>();
            TableRepository = new UsersTableRepository(TableClientProvider, _usersTableRepositoryLogger);

            PartitionKey = Constants.UsersPartitionKey;
            MergedEntityId = _mergedUserId;

            var newUser = new User(Guid.Parse(_newUserId));
            newUser.AddPlatformUserInfo(new PlatformUserInfo("PlatformName", "PlatformUserId", DataImportState.Queued));
            newUser.LastTouched = _now;
            newUser.ETag = ETag.All;
            NewEntity = newUser;
            NewEntityId = _newUserId;

            var storedUser = new User(Guid.Parse(_storedUserId));
            storedUser.AddPlatformUserInfo(new PlatformUserInfo("PlatformName", "PlatformUserId", DataImportState.ReadyToImport));
            storedUser.LastTouched = _now;
            StoredEntity = storedUser.ToTableEntity();
            StoredEntityId = _storedUserId;
        }
    }
}
