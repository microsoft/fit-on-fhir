// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Azure.Data.Tables;
using FitOnFhir.Common.Config;
using FitOnFhir.Common.Models;
using FitOnFhir.Common.Repositories;
using FitOnFhir.Common.Resolvers;
using FitOnFhir.Common.Tests.Mocks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace FitOnFhir.Common.Tests
{
    public class UsersTableRepositoryTests
    {
        private readonly AzureConfiguration _azureConfiguration;
        private TableClient _tableClient;
        private readonly MockLogger<UsersTableRepository> _usersTableRepositoryLogger;
        private IUsersTableRepository _usersTableRepository;

        private readonly DateTimeOffset _now =
            new DateTimeOffset(2004, 1, 12, 0, 0, 0, new TimeSpan(-5, 0, 0));

        private readonly DateTimeOffset _oneDayBack =
            new DateTimeOffset(2004, 1, 11, 0, 0, 0, new TimeSpan(-5, 0, 0));

        private readonly string _userId = "12345678-9101-1121-3141-516171819202";

        private Func<EntityBase, EntityBase, User> _conflictResolverFunc;
        private User _newUser;
        private User _storedUser;
        private TableEntity _newEntity;
        private TableEntity _storedEntity;

        public UsersTableRepositoryTests()
        {
            _conflictResolverFunc = Substitute.For<Func<EntityBase, EntityBase, User>>();

            // fill in the dependencies, and create a new UsersTableRepository
            _azureConfiguration = Substitute.For<AzureConfiguration>();
            _azureConfiguration.StorageAccountConnectionString = "connection string";
            _tableClient = Substitute.For<TableClient>();
            _usersTableRepositoryLogger = Substitute.For<MockLogger<UsersTableRepository>>();
            _usersTableRepository = new UsersTableRepository(_azureConfiguration, _tableClient, _usersTableRepositoryLogger);
        }

        public string PlatformName => "platformName";

        public string PlatformUserId => "platformUserId";

        [Theory]
        [InlineData(DataImportState.ReadyToImport, DataImportState.Unauthorized, DataImportState.Unauthorized)]
        [InlineData(DataImportState.Unauthorized, DataImportState.ReadyToImport, DataImportState.Unauthorized)]
        [InlineData(DataImportState.Queued, DataImportState.Queued, DataImportState.ReadyToImport)]
        [InlineData(DataImportState.Queued, DataImportState.Importing, DataImportState.ReadyToImport)]
        [InlineData(DataImportState.Queued, DataImportState.ReadyToImport, DataImportState.ReadyToImport)]
        [InlineData(DataImportState.Importing, DataImportState.Queued, DataImportState.ReadyToImport)]
        [InlineData(DataImportState.Importing, DataImportState.Importing, DataImportState.ReadyToImport)]
        [InlineData(DataImportState.Importing, DataImportState.ReadyToImport, DataImportState.ReadyToImport)]
        [InlineData(DataImportState.ReadyToImport, DataImportState.Queued, DataImportState.ReadyToImport)]
        [InlineData(DataImportState.ReadyToImport, DataImportState.Importing, DataImportState.ReadyToImport)]
        [InlineData(DataImportState.ReadyToImport, DataImportState.ReadyToImport, DataImportState.ReadyToImport)]
        public async Task GivenRequestFailedExceptionOccurs_WhenUpdateIsCalledWithResolveConflictDefault_MergedUserImportStateSetAppropriately(
            DataImportState newImportState,
            DataImportState storedImportState,
            DataImportState mergedImoprtState)
        {
            SetupTableClient(newImportState, storedImportState);

            // Arrange for UpdateEntityAsync to throw a RequestFailedException, in order for the _conflictResolverFunc to be called
            var exceptionMsg = "request failed exception";
            var exception = new RequestFailedException(412, exceptionMsg);
            _tableClient.UpdateEntityAsync(Arg.Any<TableEntity>(), Arg.Is<ETag>(tag => tag == ETag.All), cancellationToken: Arg.Any<CancellationToken>())
                .Throws(exception);

            // Act on the Update method
            _conflictResolverFunc = UserConflictResolvers.ResolveConflictDefault;
            var mergedUser = await _usersTableRepository.Update(_newUser, CancellationToken.None, _conflictResolverFunc);

            // Assert the the merged user's Platform DataImportState is set to Unauthorized
            mergedUser.GetPlatformImportState(PlatformName, out var mergedDataImportState);
            Assert.Equal(mergedImoprtState, mergedDataImportState);

            // Assert the LastTouched time is the latest
        }

        [Theory]
        [InlineData(DataImportState.ReadyToImport, DataImportState.Unauthorized, DataImportState.ReadyToImport)]
        [InlineData(DataImportState.ReadyToImport, DataImportState.Queued, DataImportState.Queued)]
        [InlineData(DataImportState.ReadyToImport, DataImportState.Importing, DataImportState.Importing)]
        [InlineData(DataImportState.ReadyToImport, DataImportState.ReadyToImport, DataImportState.ReadyToImport)]
        public async Task GivenRequestFailedExceptionOccurs_WhenUpdateIsCalledWithResolveConflictAuthorization_MergedUserImportStateSetAppropriately(
            DataImportState newImportState,
            DataImportState storedImportState,
            DataImportState mergedImoprtState)
        {
            SetupTableClient(newImportState, storedImportState);

            // Arrange for UpdateEntityAsync to throw a RequestFailedException, in order for the _conflictResolverFunc to be called
            var exceptionMsg = "request failed exception";
            var exception = new RequestFailedException(412, exceptionMsg);
            _tableClient.UpdateEntityAsync(Arg.Any<TableEntity>(), Arg.Is<ETag>(tag => tag == ETag.All), cancellationToken: Arg.Any<CancellationToken>())
                .Throws(exception);

            // Act on the Update method
            _conflictResolverFunc = UserConflictResolvers.ResolveConflictAuthorization;
            var mergedUser = await _usersTableRepository.Update(_newUser, CancellationToken.None, _conflictResolverFunc);

            // Assert the the merged user's Platform DataImportState is set to Unauthorized
            mergedUser.GetPlatformImportState(PlatformName, out var mergedDataImportState);
            Assert.Equal(mergedImoprtState, mergedDataImportState);

            // Assert the LastTouched time is the latest
        }

        private void SetupTableClient(DataImportState newImportState, DataImportState storedImportState)
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

            _newUser = new User(Guid.Parse(_userId));
            _newUser.AddPlatformUserInfo(new PlatformUserInfo(PlatformName, PlatformUserId, newImportState));
            _newUser.LastTouched = _now;
            _newEntity = _newUser.ToTableEntity();
            _newEntity.ETag = ETag.All;

            _storedUser = new User(Guid.Parse(_userId));
            _storedUser.AddPlatformUserInfo(new PlatformUserInfo(PlatformName, PlatformUserId, storedImportState));
            _storedUser.LastTouched = _oneDayBack;
            _storedEntity = _storedUser.ToTableEntity();

            Response<TableEntity> getEntityAsyncResponse = Substitute.For<Response<TableEntity>>();
            getEntityAsyncResponse.Value.ReturnsForAnyArgs(_storedEntity);
            _tableClient.GetEntityAsync<TableEntity>(Arg.Any<string>(), Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
                .Returns(getEntityAsyncResponse);
        }
    }
}
