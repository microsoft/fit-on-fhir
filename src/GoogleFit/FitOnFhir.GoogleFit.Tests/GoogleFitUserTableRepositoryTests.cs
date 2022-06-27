// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Azure.Data.Tables;
using FitOnFhir.Common.Config;
using FitOnFhir.Common.Models;
using FitOnFhir.Common.Tests.Mocks;
using FitOnFhir.GoogleFit.Client.Models;
using FitOnFhir.GoogleFit.Repositories;
using FitOnFhir.GoogleFit.Resolvers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace FitOnFhir.GoogleFit.Tests
{
    public class GoogleFitUserTableRepositoryTests
    {
        private readonly AzureConfiguration _azureConfiguration;
        private TableClient _tableClient;
        private readonly MockLogger<GoogleFitUserTableRepository> _googleFitUserTableRepositoryLogger;
        private IGoogleFitUserTableRepository _googleFitUsersTableRepository;

        private readonly DateTimeOffset _now =
            new DateTimeOffset(2004, 1, 12, 0, 0, 0, new TimeSpan(-5, 0, 0));

        private readonly DateTimeOffset _oneDayBack =
            new DateTimeOffset(2004, 1, 11, 0, 0, 0, new TimeSpan(-5, 0, 0));

        private Func<EntityBase, EntityBase, GoogleFitUser> _conflictResolverFunc;
        private GoogleFitUser _newGoogleFitUser;
        private GoogleFitUser _storedGoogleFitUser;
        private TableEntity _newEntity;
        private TableEntity _storedEntity;

        public GoogleFitUserTableRepositoryTests()
        {
            _conflictResolverFunc = Substitute.For<Func<EntityBase, EntityBase, GoogleFitUser>>();

            // fill in the dependencies, and create a new UsersTableRepository
            _azureConfiguration = Substitute.For<AzureConfiguration>();
            _azureConfiguration.StorageAccountConnectionString = "connection string";
            _tableClient = Substitute.For<TableClient>();
            _googleFitUserTableRepositoryLogger = Substitute.For<MockLogger<GoogleFitUserTableRepository>>();
            _googleFitUsersTableRepository = new GoogleFitUserTableRepository(_azureConfiguration, _tableClient, _googleFitUserTableRepositoryLogger);
        }

        public string PlatformName => "platformName";

        public string PlatformUserId => "platformUserId";

        public string DataStreamId => "dataStreamId";

        public long LaterSyncTimeNanos => long.MaxValue;

        public long EarlierSyncTimeNanos => long.MinValue;

        [Fact]
        public async Task GivenRequestFailedExceptionOccurs_WhenUpdateIsCalledWithResolveConflictDefault_MergedUserImportStateSetAppropriately()
        {
            SetupTableClient();

            // Arrange for UpdateEntityAsync to throw a RequestFailedException, in order for the _conflictResolverFunc to be called
            var exceptionMsg = "request failed exception";
            var exception = new RequestFailedException(412, exceptionMsg);
            _tableClient.UpdateEntityAsync(Arg.Any<TableEntity>(), Arg.Is<ETag>(tag => tag == ETag.All), cancellationToken: Arg.Any<CancellationToken>())
                .Throws(exception);

            // Act on the Update method
            _conflictResolverFunc = GoogleFitUserConflictResolvers.ResolveConflictLastSyncTimes;
            var mergedUser = await _googleFitUsersTableRepository.Update(_newGoogleFitUser, CancellationToken.None, _conflictResolverFunc);

            // Assert the the merged user's last sync time is correct
            mergedUser.TryGetLastSyncTime(DataStreamId, out var lastSyncTimeNanos);

            Assert.Equal(LaterSyncTimeNanos, lastSyncTimeNanos);
        }

        private void SetupTableClient()
        {
            AsyncPageable<TableEntity> fakeAsyncPageable = Substitute.For<AsyncPageable<TableEntity>>();
            _tableClient.QueryAsync<TableEntity>(cancellationToken: Arg.Any<CancellationToken>())
                .Returns(fakeAsyncPageable);

            var fakeResponse = Substitute.For<Response>();
            _tableClient.AddEntityAsync(Arg.Any<TableEntity>(), cancellationToken: Arg.Any<CancellationToken>())
                .Returns(fakeResponse);
            _tableClient.UpdateEntityAsync(Arg.Any<TableEntity>(), Arg.Any<ETag>(), cancellationToken: Arg.Any<CancellationToken>())
                .Returns(fakeResponse);
            _tableClient.UpsertEntityAsync(Arg.Any<TableEntity>(), cancellationToken: Arg.Any<CancellationToken>())
                .Returns(fakeResponse);
            _tableClient.DeleteEntityAsync(Arg.Any<string>(), Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
                .Returns(fakeResponse);

            _newGoogleFitUser = new GoogleFitUser(PlatformUserId);
            _newGoogleFitUser.SaveLastSyncTime(DataStreamId, LaterSyncTimeNanos);
            _newEntity = _newGoogleFitUser.ToTableEntity();
            _newEntity.ETag = ETag.All;

            _storedGoogleFitUser = new GoogleFitUser(PlatformUserId);
            _storedGoogleFitUser.SaveLastSyncTime(DataStreamId, EarlierSyncTimeNanos);
            _storedEntity = _storedGoogleFitUser.ToTableEntity();

            Response<TableEntity> getEntityAsyncResponse = Substitute.For<Response<TableEntity>>();
            getEntityAsyncResponse.Value.ReturnsForAnyArgs(_storedEntity);
            _tableClient.GetEntityAsync<TableEntity>(Arg.Any<string>(), Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
                .Returns(getEntityAsyncResponse);
        }
    }
}
