// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Azure.Data.Tables;
using Microsoft.Health.FitOnFhir.Common.Models;
using Microsoft.Health.FitOnFhir.Common.Providers;
using Microsoft.Health.FitOnFhir.Common.Repositories;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.FitOnFhir.Common.Tests
{
    public abstract class TableRepositoryBaseTests<T>
        where T : EntityBase
    {
        private Func<T, T, T> _conflictResolverFunc;
        private T _mergedEntity;
        private T _arg1Entity;
        private T _arg2Entity;

        public TableRepositoryBaseTests()
        {
            TableClient = Substitute.For<TableClient>();
            TableClientProvider = Substitute.For<ITableClientProvider>();
            TableClientProvider.GetTableClient(Arg.Any<string>()).Returns(TableClient);
            _mergedEntity = Substitute.For<T>();

            _conflictResolverFunc = (arg1, arg2) =>
            {
                _arg1Entity = arg1;
                _arg2Entity = arg2;
                return _mergedEntity;
            };
        }

        protected ITableClientProvider TableClientProvider { get; private set; }

        protected TableClient TableClient { get; private set; }

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

        protected int MatchingExceptionStatusCode => 412;

        protected int MismatchedExceptionStatusCode => 410;

        [Fact]
        public async Task GivenRequestFailedExceptionOccurs_WhenUpdateIsCalled_ConflictResolverIsCalledWithCorrectParams()
        {
            SetupTableClient(MatchingExceptionStatusCode);

            var mergedEntity = await TableRepository.Update(NewEntity, _conflictResolverFunc, CancellationToken.None);

            Assert.NotNull(_arg1Entity);
            Assert.Equal(NewEntityId, _arg1Entity.Id);
            Assert.NotNull(_arg2Entity);
            Assert.Equal(StoredEntityId, _arg2Entity.Id);
        }

        [Fact]
        public async Task GivenRequestFailedExceptionOccurs_WhenUpdateIsCalled_GetEntityAsyncIsCalledWithCorrectParams()
        {
            SetupTableClient(MatchingExceptionStatusCode);

            _ = await TableRepository.Update(NewEntity, _conflictResolverFunc, CancellationToken.None);

            await TableClient.Received(1).GetEntityAsync<TableEntity>(
                Arg.Is<string>(str => str == PartitionKey),
                Arg.Is<string>(str => str == NewEntityId),
                cancellationToken: Arg.Any<CancellationToken>());

            await TableClient.Received(1).GetEntityAsync<TableEntity>(
                Arg.Is<string>(str => str == PartitionKey),
                Arg.Is<string>(str => str == MergedEntityId),
                cancellationToken: Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GivenRequestFailedExceptionOccursWithNonMatchingStatus_WhenUpdateIsCalled_ThrowsNonMatchingException()
        {
            SetupTableClient(MismatchedExceptionStatusCode);

            await Assert.ThrowsAsync<RequestFailedException>(async () => await TableRepository.Update(NewEntity, _conflictResolverFunc, CancellationToken.None));
        }

        public void SetupTableClient(int exceptionStatusCode)
        {
            AsyncPageable<TableEntity> fakeAsyncPageable = Substitute.For<AsyncPageable<TableEntity>>();
            TableClient.QueryAsync<TableEntity>(cancellationToken: Arg.Any<CancellationToken>())
                .Returns(fakeAsyncPageable);

            var fakeResponse = Substitute.For<Response>();
            TableClient.AddEntityAsync(Arg.Any<TableEntity>(), cancellationToken: Arg.Any<CancellationToken>())
                .Returns(fakeResponse);

            var exceptionMsg = "request failed exception";
            var exception = new RequestFailedException(exceptionStatusCode, exceptionMsg);
            TableClient.UpdateEntityAsync(
                    Arg.Any<TableEntity>(),
                    Arg.Any<ETag>(),
                    cancellationToken: Arg.Any<CancellationToken>())
                .Returns(x => throw exception, x => fakeResponse);

            TableClient.UpsertEntityAsync(Arg.Any<TableEntity>(), cancellationToken: Arg.Any<CancellationToken>())
                .Returns(fakeResponse);
            TableClient.DeleteEntityAsync(Arg.Any<string>(), Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
                .Returns(fakeResponse);

            Response<TableEntity> getEntityAsyncResponse = Substitute.For<Response<TableEntity>>();
            getEntityAsyncResponse.Value.ReturnsForAnyArgs(StoredEntity);
            TableClient.GetEntityAsync<TableEntity>(Arg.Any<string>(), Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
                .Returns(getEntityAsyncResponse);
        }
    }
}
