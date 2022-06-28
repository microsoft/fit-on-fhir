// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Azure.Data.Tables;
using FitOnFhir.Common.Config;
using NSubstitute;

namespace FitOnFhir.Common.Tests
{
    public abstract class UserTableRepositoryBaseTests
    {
        private TableClient _tableClient;
        private TableEntity _storedEntity;
        private AzureConfiguration _azureConfig;

        public UserTableRepositoryBaseTests()
        {
            _tableClient = Substitute.For<TableClient>();
            _azureConfig = Substitute.For<AzureConfiguration>();
            _azureConfig.StorageAccountConnectionString = "connection string";
        }

        protected AzureConfiguration AzureConfig => _azureConfig;

        protected string PlatformName => "platformName";

        protected string PlatformUserId => "platformUserId";

        protected TableClient TableClient => _tableClient;

        protected TableEntity StoredEntity
        {
            get { return _storedEntity; }
            set => _storedEntity = value;
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

            SetupGetEntityAsyncReturn();
        }

        protected abstract void SetupGetEntityAsyncReturn();
    }
}
