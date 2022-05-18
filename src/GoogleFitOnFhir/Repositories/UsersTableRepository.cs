// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using FitOnFhir.Common.Models;
using GoogleFitOnFhir.Persistence;
using Microsoft.Extensions.Logging;

namespace GoogleFitOnFhir.Repositories
{
    public class UsersTableRepository : IUsersTableRepository
    {
        private readonly StorageAccountContext _storageAccountContext;
        private readonly TableClient _tableClient;
        private readonly ILogger<UsersTableRepository> _logger;

        public UsersTableRepository(StorageAccountContext storageAccountContext, ILogger<UsersTableRepository> logger)
        {
            _storageAccountContext = storageAccountContext;
            _tableClient = new TableClient(storageAccountContext.ConnectionString, "users");
            _logger = logger;
        }

        public AsyncPageable<User> GetAll(CancellationToken cancellationToken)
        {
            return _tableClient.QueryAsync<User>(cancellationToken: cancellationToken);
        }

        public async Task<User> GetById(string id, CancellationToken cancellationToken)
        {
            return await _tableClient.GetEntityAsync<User>(id, id, cancellationToken: cancellationToken);
        }

        public async Task Insert(User user, CancellationToken cancellationToken)
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

        public async Task Update(User user, CancellationToken cancellationToken)
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

        public async Task Upsert(User user, CancellationToken cancellationToken)
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

        public async Task Delete(User user, CancellationToken cancellationToken)
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