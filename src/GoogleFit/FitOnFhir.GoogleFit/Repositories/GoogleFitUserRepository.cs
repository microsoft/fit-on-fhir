// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Azure.Data.Tables;
using FitOnFhir.Common.Persistence;
using FitOnFhir.GoogleFit.Client.Models;
using Microsoft.Extensions.Logging;

namespace FitOnFhir.GoogleFit.Repositories
{
    public class GoogleFitUserRepository : IGoogleFitUserRepository
    {
        private readonly StorageAccountContext _storageAccountContext;
        private readonly TableClient _tableClient;
        private readonly ILogger<GoogleFitUserRepository> _logger;

        public GoogleFitUserRepository(StorageAccountContext storageAccountContext, ILogger<GoogleFitUserRepository> logger)
        {
            _storageAccountContext = storageAccountContext;
            _tableClient = new TableClient(storageAccountContext.ConnectionString, "users");
            _logger = logger;
        }

        public AsyncPageable<GoogleFitUser> GetAll(CancellationToken cancellationToken)
        {
            return _tableClient.QueryAsync<GoogleFitUser>(cancellationToken: cancellationToken);
        }

        public async Task<GoogleFitUser> GetById(string id, CancellationToken cancellationToken)
        {
            return await _tableClient.GetEntityAsync<GoogleFitUser>(id, id, cancellationToken: cancellationToken);
        }

        public async Task Insert(GoogleFitUser user, CancellationToken cancellationToken)
        {
            try
            {
                await _tableClient.AddEntityAsync(user, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        public async Task Update(GoogleFitUser user, CancellationToken cancellationToken)
        {
            try
            {
                await _tableClient.UpdateEntityAsync(user, user.ETag, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        public async Task Upsert(GoogleFitUser user, CancellationToken cancellationToken)
        {
            try
            {
                await _tableClient.UpsertEntityAsync(user, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        public async Task Delete(GoogleFitUser user, CancellationToken cancellationToken)
        {
            try
            {
                await _tableClient.DeleteEntityAsync(user.PartitionKey, user.RowKey, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}
