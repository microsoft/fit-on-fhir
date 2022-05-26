// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Azure.Data.Tables;
using FitOnFhir.Common.Persistence;
using Microsoft.Extensions.Logging;

namespace FitOnFhir.Common.Repositories
{
    public abstract class TableRepository<TEntity>
        where TEntity : class, ITableEntity, new()
    {
        private readonly TableClient _tableClient;
        private readonly ILogger _logger;

        public TableRepository(StorageAccountContext storageAccountContext, ILogger logger)
        {
            _tableClient = new TableClient(storageAccountContext.ConnectionString, "users");
            _logger = logger;
        }

        public string PartitionKey { get; set; }

        public AsyncPageable<TEntity> GetAll(CancellationToken cancellationToken)
        {
            return _tableClient.QueryAsync<TEntity>(cancellationToken: cancellationToken);
        }

        public async Task<TEntity> GetById(string id, CancellationToken cancellationToken)
        {
            return await _tableClient.GetEntityAsync<TEntity>(PartitionKey, id, cancellationToken: cancellationToken);
        }

        public async Task Insert(TEntity entity, CancellationToken cancellationToken)
        {
            try
            {
                await _tableClient.AddEntityAsync(entity, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        public async Task Update(TEntity entity, CancellationToken cancellationToken)
        {
            try
            {
                await _tableClient.UpdateEntityAsync(entity, entity.ETag, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        public async Task Upsert(TEntity entity, CancellationToken cancellationToken)
        {
            try
            {
                await _tableClient.UpsertEntityAsync(entity, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        public async Task Delete(TEntity entity, CancellationToken cancellationToken)
        {
            try
            {
                await _tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}
