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
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.Routing.Handlers;
using Xunit;

namespace FitOnFhir.Common.Tests
{
    public abstract class TableRepositoryBaseTests<T>
        where T : EntityBase
    {
        private TableClient _tableClient;
        private AzureConfiguration _azureConfig;
        private Func<T, T, T> _conflictResolverFunc;
        private T _mergedEntity;
        private T _arg1Entity;
        private T _arg2Entity;

        public TableRepositoryBaseTests()
        {
            _tableClient = Substitute.For<TableClient>();
            _azureConfig = Substitute.For<AzureConfiguration>();
            _azureConfig.StorageAccountConnectionString = "connection string";
            _mergedEntity = Substitute.For<T>();

            _conflictResolverFunc = (arg1, arg2) =>
            {
                _arg1Entity = arg1;
                _arg2Entity = arg2;
                return _mergedEntity;
            };
        }

        protected AzureConfiguration AzureConfig => _azureConfig;

        protected TableClient TableClient => _tableClient;

        protected ITableRepository<T> TableRepository { get; set; }

        protected T NewEntity { get; set; }

        protected string NewEntityId
        {
            get { return NewEntity.Id; }
            set => NewEntity.Id = value;
        }

        protected TableEntity StoredEntity { get; set; }

        protected string StoredEntityId { get; set; }

        protected string MergedEntityId
        {
            get { return _mergedEntity.Id; }
            set => _mergedEntity.Id = value;
        }

        protected string PartitionKey { get; set; }

        [Fact]
        public async Task GivenRequestFailedExceptionOccurs_WhenUpdateIsCalled_ConflictResolverIsCalledWithCorrectParams()
        {
            SetupTableClient();

            // Arrange for UpdateEntityAsync to throw a RequestFailedException, in order for the _conflictResolverFunc to be called
            var exceptionMsg = "request failed exception";
            var exception = new RequestFailedException(412, exceptionMsg);
            TableClient.UpdateEntityAsync(
                    Arg.Any<TableEntity>(),
                    Arg.Is<ETag>(tag => tag == ETag.All),
                    cancellationToken: Arg.Any<CancellationToken>())
                .Throws(exception);

            // Act on the Update method
            var mergedEntity = await TableRepository.Update(NewEntity, _conflictResolverFunc, CancellationToken.None);

            // Assert the entities were assigned in the conflict resolver
            Assert.NotNull(_arg1Entity);
            Assert.Equal(NewEntityId, _arg1Entity.Id);
            Assert.NotNull(_arg2Entity);
            Assert.Equal(StoredEntityId, _arg2Entity.Id);
        }

        [Fact]
        public async Task GivenRequestFailedExceptionOccurs_WhenUpdateIsCalled_GetEntityAsyncIsCalledWithCorrectParams()
        {
            SetupTableClient();

            // Arrange for UpdateEntityAsync to throw a RequestFailedException, in order for the _conflictResolverFunc to be called
            var exceptionMsg = "request failed exception";
            var exception = new RequestFailedException(412, exceptionMsg);
            TableClient.UpdateEntityAsync(
                    Arg.Any<TableEntity>(),
                    Arg.Is<ETag>(tag => tag == ETag.All),
                    cancellationToken: Arg.Any<CancellationToken>())
                .Throws(exception);

            // Act on the Update method
            _ = await TableRepository.Update(NewEntity, _conflictResolverFunc, CancellationToken.None);

            // Assert one call was for the NewEntity, and one was for the MergedEntity
            await TableClient.Received(1).GetEntityAsync<TableEntity>(
                Arg.Is<string>(str => str == PartitionKey),
                Arg.Is<string>(str => str == NewEntityId),
                cancellationToken: Arg.Any<CancellationToken>());

            await TableClient.Received(1).GetEntityAsync<TableEntity>(
                Arg.Is<string>(str => str == PartitionKey),
                Arg.Is<string>(str => str == MergedEntityId),
                cancellationToken: Arg.Any<CancellationToken>());
        }

        public void SetupTableClient()
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

            Response<TableEntity> getEntityAsyncResponse = Substitute.For<Response<TableEntity>>();
            getEntityAsyncResponse.Value.ReturnsForAnyArgs(StoredEntity);
            TableClient.GetEntityAsync<TableEntity>(Arg.Any<string>(), Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
                .Returns(getEntityAsyncResponse);
        }
    }
}
