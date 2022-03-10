// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Azure.Data.Tables;
using GoogleFitOnFhir.Models;
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

        public IEnumerable<User> GetAll()
        {
            return _tableClient.Query<User>();
        }

        public User GetById(string id)
        {
            return _tableClient.GetEntity<User>(id, id);
        }

        public void Insert(User user)
        {
            try
            {
                _tableClient.AddEntity(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public void Update(User user)
        {
            try
            {
                _tableClient.UpdateEntity(user, user.ETag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public void Upsert(User user)
        {
            try
            {
                _tableClient.UpsertEntity(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public void Delete(User user)
        {
            try
            {
                _tableClient.DeleteEntity(user.PartitionKey, user.RowKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}