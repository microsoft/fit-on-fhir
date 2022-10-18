// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Microsoft.Health.FitOnFhir.Common.Tests;
using Microsoft.Health.FitOnFhir.Common.Tests.Mocks;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Models;
using Microsoft.Health.FitOnFhir.GoogleFit.Common;
using Microsoft.Health.FitOnFhir.GoogleFit.Repositories;
using NSubstitute;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Tests
{
    public class GoogleFitUserTableRepositoryTests : TableRepositoryBaseTests<GoogleFitUser>
    {
        private readonly MockLogger<GoogleFitUserTableRepository> _googleFitUserTableRepositoryLogger;
        private readonly string _mergedUserId = "mergedId";
        private readonly string _newUserId = "newId";
        private readonly string _storedUserId = "storedId";

        public GoogleFitUserTableRepositoryTests()
        {
            _googleFitUserTableRepositoryLogger = Substitute.For<MockLogger<GoogleFitUserTableRepository>>();
            TableRepository = new GoogleFitUserTableRepository(TableClientProvider, _googleFitUserTableRepositoryLogger);

            PartitionKey = GoogleFitConstants.GoogleFitPartitionKey;
            MergedEntityId = _mergedUserId;

            var newUser = new GoogleFitUser(_newUserId);
            newUser.SaveLastSyncTime("dataStreamId", long.MaxValue);
            newUser.ETag = ETag.All;
            NewEntity = newUser;
            NewEntityId = _newUserId;

            var storedUser = new GoogleFitUser(_storedUserId);
            storedUser.SaveLastSyncTime("dataStreamId", long.MinValue);
            StoredEntity = storedUser.ToTableEntity();
            StoredEntityId = _storedUserId;
        }
    }
}
