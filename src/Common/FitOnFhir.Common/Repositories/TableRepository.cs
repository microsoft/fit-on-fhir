// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Azure.Data.Tables;
using EnsureThat;
using FitOnFhir.Common.Models;
using FitOnFhir.Common.Persistence;
using Microsoft.Extensions.Logging;

namespace FitOnFhir.Common.Repositories
{
    public abstract class TableRepository<TEntity>
        where TEntity : EntityBase, new()
    {
        private readonly TableClient _tableClient;
        private readonly ILogger _logger;

        public TableRepository(string connectionString, ILogger logger)
        {
            EnsureArg.IsNotNullOrWhiteSpace(connectionString, nameof(connectionString));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _tableClient = new TableClient(connectionString, Constants.UsersTableName);
            _logger = logger;
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

        public async Task Insert(TEntity entity, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(entity, nameof(entity));

            try
            {
                await _tableClient.AddEntityAsync(entity.ToTableEntity(), cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        public async Task Update(TEntity entity, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(entity, nameof(entity));

            try
            {
                await _tableClient.UpdateEntityAsync(entity.ToTableEntity(), entity.ETag, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        public async Task Upsert(TEntity entity, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(entity, nameof(entity));

            try
            {
                await _tableClient.UpsertEntityAsync(entity.ToTableEntity(), cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        public async Task Delete(TEntity entity, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(entity, nameof(entity));

            try
            {
                await _tableClient.DeleteEntityAsync(entity.PartitionKey, entity.Id, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}
