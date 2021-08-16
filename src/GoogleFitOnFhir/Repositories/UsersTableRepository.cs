using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Data.Tables;
using GoogleFitOnFhir.Models;
using GoogleFitOnFhir.Persistence;
using Microsoft.Extensions.Logging;

namespace GoogleFitOnFhir.Repositories
{
    public class UsersTableRepository : IUsersTableRepository
    {
        private StorageAccountContext storageAccountContext;

        private TableClient tableClient;

        private ILogger<UsersTableRepository> logger;

        public UsersTableRepository(StorageAccountContext storageAccountContext, ILogger<UsersTableRepository> logger)
        {
            this.storageAccountContext = storageAccountContext;
            this.tableClient = new TableClient(this.storageAccountContext.ConnectionString, "users");
            this.logger = logger;
        }

        public IEnumerable<User> GetAll()
        {
            return this.tableClient.Query<User>();
        }

        public User GetById(string id)
        {
            return this.tableClient.GetEntity<User>(id, id);
        }

        public void Insert(User user)
        {
            try
            {
                this.tableClient.AddEntity(user);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex.Message);
            }
        }

        public void Update(User user)
        {
            try
            {
                this.tableClient.UpdateEntity(user, user.ETag);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex.Message);
            }
        }

        public void Upsert(User user)
        {
            try
            {
                this.tableClient.UpsertEntity(user);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex.Message);
            }
        }

        public void Delete(User user)
        {
            try
            {
                this.tableClient.DeleteEntity(user.PartitionKey, user.RowKey);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex.Message);
            }
        }
    }
}