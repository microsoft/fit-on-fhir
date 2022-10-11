// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Azure.Data.Tables;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.FitOnFhir.Common.Models;
using Microsoft.Health.FitOnFhir.Common.Providers;

namespace Microsoft.Health.FitOnFhir.Common.Repositories
{
    public abstract class TableRepository<TEntity>
        where TEntity : EntityBase, new()
    {
        private readonly TableClient _tableClient;
        private readonly ILogger _logger;

        protected TableRepository(ITableClientProvider tableClientProvider, ILogger logger)
        {
            _tableClient = EnsureArg.IsNotNull(tableClientProvider, nameof(tableClientProvider)).GetTableClient(Constants.UsersTableName);
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        public string PartitionKey { get; set; }

        public AsyncPageable<TableEntity> GetAll(CancellationToken cancellationToken)
        {
            return _tableClient.QueryAsync<TableEntity>(cancellationToken: cancellationToken);
        }

        public async Task<TEntity> GetById(string id, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(id, nameof(id));

            try
            {
                var response = await _tableClient.GetEntityAsync<TableEntity>(PartitionKey, id, cancellationToken: cancellationToken);

                if (response?.Value != null)
                {
                    return (TEntity)Activator.CreateInstance(typeof(TEntity), response.Value);
                }
            }
            catch (RequestFailedException e) when (e.Status == 404)
            {
            }

            return null;
        }

        public async Task<TEntity> Insert(TEntity entity, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(entity, nameof(entity));

            await _tableClient.AddEntityAsync(entity.ToTableEntity(), cancellationToken: cancellationToken);

            return await GetById(entity.Id, cancellationToken);
        }

        public async Task<TEntity> Update(TEntity entity, Func<TEntity, TEntity, TEntity> conflictResolver, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(entity, nameof(entity));
            EnsureArg.IsNotNull(conflictResolver, nameof(conflictResolver));

            try
            {
                await _tableClient.UpdateEntityAsync(entity.ToTableEntity(), entity.ETag, cancellationToken: cancellationToken);
            }
            catch (RequestFailedException ex) when (ex.Status == 412)
            {
                _logger.LogError(ex, ex.Message);

                TEntity storedEntity = await GetById(entity.Id, cancellationToken);
                TEntity mergedEntity = conflictResolver(entity, storedEntity);
                return await Update(mergedEntity, conflictResolver, cancellationToken);
            }

            return await GetById(entity.Id, cancellationToken);
        }

        public async Task<TEntity> Upsert(TEntity entity, Func<TEntity, TEntity, TEntity> conflictResolver, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(entity, nameof(entity));
            EnsureArg.IsNotNull(conflictResolver, nameof(conflictResolver));

            try
            {
                await _tableClient.UpsertEntityAsync(entity.ToTableEntity(), cancellationToken: cancellationToken);
            }
            catch (RequestFailedException ex) when (ex.Status == 412)
            {
                _logger.LogError(ex, ex.Message);

                TEntity storedEntity = await GetById(entity.Id, cancellationToken);
                TEntity mergedEntity = conflictResolver(entity, storedEntity);
                return await Upsert(mergedEntity, conflictResolver, cancellationToken);
            }

            return await GetById(entity.Id, cancellationToken);
        }

        public async Task Delete(TEntity entity, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(entity, nameof(entity));

            await _tableClient.DeleteEntityAsync(entity.PartitionKey, entity.Id, cancellationToken: cancellationToken);
        }
    }
}
