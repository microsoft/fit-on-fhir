// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Azure.Data.Tables;
using FitOnFhir.Common.Models;
using FitOnFhir.Common.Repositories;
using FitOnFhir.Common.Resolvers;
using FitOnFhir.Common.Tests.Mocks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace FitOnFhir.Common.Tests
{
    public class UsersTableRepositoryTests : UserTableRepositoryBaseTests
    {
        private readonly MockLogger<UsersTableRepository> _usersTableRepositoryLogger;
        private IUsersTableRepository _usersTableRepository;
        private Func<EntityBase, EntityBase, User> _conflictResolverFunc;
        private User _newUser;
        private User _storedUser;

        private readonly DateTimeOffset _now =
            new DateTimeOffset(2004, 1, 12, 0, 0, 0, new TimeSpan(-5, 0, 0));

        private readonly DateTimeOffset _oneDayBack =
            new DateTimeOffset(2004, 1, 11, 0, 0, 0, new TimeSpan(-5, 0, 0));

        public UsersTableRepositoryTests()
        {
            _conflictResolverFunc = Substitute.For<Func<EntityBase, EntityBase, User>>();

            // create a new UsersTableRepository
            _usersTableRepositoryLogger = Substitute.For<MockLogger<UsersTableRepository>>();
            _usersTableRepository = new UsersTableRepository(AzureConfig, TableClient, _usersTableRepositoryLogger);
        }

        protected DataImportState NewImportState { get; set; }

        protected DataImportState StoredImportState { get; set; }

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
            DataImportState mergedImportState)
        {
            NewImportState = newImportState;
            StoredImportState = storedImportState;
            SetupTableClient();

            // Arrange for UpdateEntityAsync to throw a RequestFailedException, in order for the _conflictResolverFunc to be called
            var exceptionMsg = "request failed exception";
            var exception = new RequestFailedException(412, exceptionMsg);
            TableClient.UpdateEntityAsync(Arg.Any<TableEntity>(), Arg.Is<ETag>(tag => tag == ETag.All), cancellationToken: Arg.Any<CancellationToken>())
                .Throws(exception);

            // Act on the Update method
            _conflictResolverFunc = UserConflictResolvers.ResolveConflictDefault;
            var mergedUser = await _usersTableRepository.Update(_newUser, CancellationToken.None, _conflictResolverFunc);

            // Assert the the merged user's Platform DataImportState is set to Unauthorized
            mergedUser.GetPlatformImportState(PlatformName, out var mergedDataImportState);
            Assert.Equal(mergedImportState, mergedDataImportState);

            // Assert the exception was logged
            _usersTableRepositoryLogger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                Arg.Any<RequestFailedException>(),
                Arg.Is<string>(msg => msg == exceptionMsg));

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
            DataImportState mergedImportState)
        {
            NewImportState = newImportState;
            StoredImportState = storedImportState;
            SetupTableClient();

            // Arrange for UpdateEntityAsync to throw a RequestFailedException, in order for the _conflictResolverFunc to be called
            var exceptionMsg = "request failed exception";
            var exception = new RequestFailedException(412, exceptionMsg);
            TableClient.UpdateEntityAsync(Arg.Any<TableEntity>(), Arg.Is<ETag>(tag => tag == ETag.All), cancellationToken: Arg.Any<CancellationToken>())
                .Throws(exception);

            // Act on the Update method
            _conflictResolverFunc = UserConflictResolvers.ResolveConflictAuthorization;
            var mergedUser = await _usersTableRepository.Update(_newUser, CancellationToken.None, _conflictResolverFunc);

            // Assert the the merged user's Platform DataImportState is set to Unauthorized
            mergedUser.GetPlatformImportState(PlatformName, out var mergedDataImportState);
            Assert.Equal(mergedImportState, mergedDataImportState);

            // Assert the exception was logged
            _usersTableRepositoryLogger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                Arg.Any<RequestFailedException>(),
                Arg.Is<string>(msg => msg == exceptionMsg));

            // Assert the LastTouched time is the latest
        }

        protected override void SetupGetEntityAsyncReturns()
        {
            string userId = "12345678-9101-1121-3141-516171819202";
            _newUser = new User(Guid.Parse(userId));
            _newUser.AddPlatformUserInfo(new PlatformUserInfo(PlatformName, PlatformUserId, NewImportState));
            _newUser.LastTouched = _now;
            NewEntity = _newUser.ToTableEntity();
            NewEntity.ETag = ETag.All;

            _storedUser = new User(Guid.Parse(userId));
            _storedUser.AddPlatformUserInfo(new PlatformUserInfo(PlatformName, PlatformUserId, StoredImportState));
            _storedUser.LastTouched = _oneDayBack;
            StoredEntity = _storedUser.ToTableEntity();

            Response<TableEntity> getEntityAsyncResponse = Substitute.For<Response<TableEntity>>();
            getEntityAsyncResponse.Value.ReturnsForAnyArgs(StoredEntity);
            TableClient.GetEntityAsync<TableEntity>(Arg.Any<string>(), Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
                .Returns(getEntityAsyncResponse);
        }
    }
}
