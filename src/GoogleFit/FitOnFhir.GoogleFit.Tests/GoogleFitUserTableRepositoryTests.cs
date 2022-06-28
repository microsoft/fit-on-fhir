// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Azure.Data.Tables;
using FitOnFhir.Common.Models;
using FitOnFhir.Common.Tests;
using FitOnFhir.Common.Tests.Mocks;
using FitOnFhir.GoogleFit.Client.Models;
using FitOnFhir.GoogleFit.Repositories;
using FitOnFhir.GoogleFit.Resolvers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace FitOnFhir.GoogleFit.Tests
{
    public class GoogleFitUserTableRepositoryTests : UserTableRepositoryBaseTests
    {
        private readonly MockLogger<GoogleFitUserTableRepository> _googleFitUserTableRepositoryLogger;
        private IGoogleFitUserTableRepository _googleFitUsersTableRepository;
        private Func<EntityBase, EntityBase, GoogleFitUser> _conflictResolverFunc;
        private GoogleFitUser _newGoogleFitUser;
        private GoogleFitUser _storedGoogleFitUser;

        public GoogleFitUserTableRepositoryTests()
        {
            _conflictResolverFunc = Substitute.For<Func<EntityBase, EntityBase, GoogleFitUser>>();

            // create a new UsersTableRepository
            _googleFitUserTableRepositoryLogger = Substitute.For<MockLogger<GoogleFitUserTableRepository>>();
            _googleFitUsersTableRepository = new GoogleFitUserTableRepository(AzureConfig, TableClient, _googleFitUserTableRepositoryLogger);
        }

        protected string DataStreamId => "dataStreamId";

        protected long LaterSyncTimeNanos => long.MaxValue;

        protected long EarlierSyncTimeNanos => long.MinValue;

        [Fact]
        public async Task GivenRequestFailedExceptionOccurs_WhenUpdateIsCalledWithResolveConflictDefault_MergedUserImportStateSetAppropriately()
        {
            CreateNewUser();
            SetupTableClient();

            // Arrange for UpdateEntityAsync to throw a RequestFailedException, in order for the _conflictResolverFunc to be called
            var exceptionMsg = "request failed exception";
            var exception = new RequestFailedException(412, exceptionMsg);
            TableClient.UpdateEntityAsync(Arg.Any<TableEntity>(), Arg.Is<ETag>(tag => tag == ETag.All), cancellationToken: Arg.Any<CancellationToken>())
                .Throws(exception);

            // Act on the Update method
            _conflictResolverFunc = GoogleFitUserConflictResolvers.ResolveConflictLastSyncTimes;
            var mergedUser = await _googleFitUsersTableRepository.Update(_newGoogleFitUser, CancellationToken.None, _conflictResolverFunc);

            // Assert the the merged user's last sync time is correct
            mergedUser.TryGetLastSyncTime(DataStreamId, out var lastSyncTimeNanos);
            Assert.Equal(LaterSyncTimeNanos, lastSyncTimeNanos);

            // Assert the exception was logged
            _googleFitUserTableRepositoryLogger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                Arg.Any<RequestFailedException>(),
                Arg.Is<string>(msg => msg == exceptionMsg));
        }

        private void CreateNewUser()
        {
            _newGoogleFitUser = new GoogleFitUser(PlatformUserId);
            _newGoogleFitUser.SaveLastSyncTime(DataStreamId, LaterSyncTimeNanos);
            _newGoogleFitUser.ETag = ETag.All;
        }

        protected override void SetupGetEntityAsyncReturn()
        {
            _storedGoogleFitUser = new GoogleFitUser(PlatformUserId);
            _storedGoogleFitUser.SaveLastSyncTime(DataStreamId, EarlierSyncTimeNanos);
            StoredEntity = _storedGoogleFitUser.ToTableEntity();

            Response<TableEntity> getEntityAsyncResponse = Substitute.For<Response<TableEntity>>();
            getEntityAsyncResponse.Value.ReturnsForAnyArgs(StoredEntity);
            TableClient.GetEntityAsync<TableEntity>(Arg.Any<string>(), Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
                .Returns(getEntityAsyncResponse);
        }
    }
}
