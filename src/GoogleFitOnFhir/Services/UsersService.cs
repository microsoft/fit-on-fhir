using System;
using System.Collections.Generic;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using GoogleFitOnFhir.Models;
using GoogleFitOnFhir.Repositories;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using GoogleFitClient = GoogleFitOnFhir.Clients.GoogleFit.Client;

namespace GoogleFitOnFhir.Services
{
    /// <summary>
    /// User Service.
    /// </summary>
    public class UsersService : IUsersService
    {
        private IUsersTableRepository usersTableRepository;

        private GoogleFitClient googleFitClient;

        private EventHubProducerClient eventHubProducerClient;

        private ILogger<UsersService> logger;

        public UsersService(
            IUsersTableRepository usersTableRepository,
            GoogleFitClient googleFitClient,
            EventHubProducerClient eventHubProducerClient,
            ILogger<UsersService> logger)
        {
            this.usersTableRepository = usersTableRepository;
            this.googleFitClient = googleFitClient;
            this.eventHubProducerClient = eventHubProducerClient;
            this.logger = logger;
        }

        public void Initiate(User user)
        {
            this.usersTableRepository.Upsert(user);
        }

        public async void ImportFitnessData(User user)
        {
            // TODO: Retrieve the accessToken from KV using user.Id
            string accessToken = string.Empty;

            // TODO: Retrieve refresh token for user
            // TODO: Store new refresh token
            var dataSourcesList = await this.googleFitClient.DatasourcesListRequest(accessToken);

            // Create a batch of events for IoMT eventhub
            using EventDataBatch eventBatch = await this.eventHubProducerClient.CreateBatchAsync();

            // Get dataset for each dataSource
            foreach (var datasourceId in dataSourcesList.DatasourceIds)
            {
                // TODO: Generate datasetId based on event type
                //       last 30 days to beginning of hour for first migration
                //       previous hour for interval migration
                var dataset = await this.googleFitClient.DatasetRequest(
                    accessToken,
                    datasourceId,
                    "1574159699023000000-1574159699023000000");

                // Add user id to payload
                // TODO: Use userId from queue message
                dataset.UserId = "testUserId";

                // Push dataset to IoMT connector
                if (!eventBatch.TryAdd(new EventData(JsonConvert.SerializeObject(dataset))))
                {
                    throw new Exception("Event is too large for the batch and cannot be sent.");
                }
            }

            try
            {
                // Use the producer client to send the batch of events to the event hub
                await this.eventHubProducerClient.SendAsync(eventBatch);
                this.logger.LogInformation("A batch of events has been published.");

                // Update LastSync column
                user.LastSync = DateTime.Now;
                this.usersTableRepository.Update(user);
            }
            finally
            {
                await this.eventHubProducerClient.DisposeAsync();
            }
        }

        public void QueueFitnessImport(User user)
        {
        }
    }
}
