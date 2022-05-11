// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using GoogleFitOnFhir.Clients.GoogleFit;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;
using GoogleFitOnFhir.Repositories;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GoogleFitOnFhir.Services
{
    public class GoogleFitDataImporter : IGoogleFitDataImporter, IAsyncDisposable
    {
        private readonly IUsersTableRepository _usersTableRepository;
        private readonly IGoogleFitClient _googleFitClient;
        private readonly EventHubProducerClient _eventHubProducerClient;
        private readonly ILogger<GoogleFitDataImporter> _logger;
        private readonly IUsersKeyVaultRepository _usersKeyvaultRepository;
        private readonly IGoogleFitAuthService _googleFitAuthService;

        public GoogleFitDataImporter(
            IUsersTableRepository usersTableRepository,
            IGoogleFitClient googleFitClient,
            EventHubProducerClient eventHubProducerClient,
            IUsersKeyVaultRepository usersKeyvaultRepository,
            IGoogleFitAuthService googleFitAuthService,
            ILogger<GoogleFitDataImporter> logger)
        {
            _usersTableRepository = usersTableRepository;
            _googleFitClient = googleFitClient;
            _eventHubProducerClient = eventHubProducerClient;
            _usersKeyvaultRepository = usersKeyvaultRepository;
            _googleFitAuthService = googleFitAuthService;
            _logger = logger;
        }

        public async Task Import(string userId, CancellationToken cancellationToken)
        {
            string refreshToken;

            _logger.LogInformation("Get RefreshToken from KV for {0}", userId);

            try
            {
                refreshToken = await _usersKeyvaultRepository.GetByName(userId, cancellationToken);
            }
            catch (AggregateException ex)
            {
                _logger.LogError(ex.Message);
                return;
            }

            _logger.LogInformation("Refreshing the RefreshToken");
            AuthTokensResponse tokensResponse = await _googleFitAuthService.RefreshTokensRequest(refreshToken, cancellationToken);

            if (!string.IsNullOrEmpty(tokensResponse.RefreshToken))
            {
                _logger.LogInformation("Updating refreshToken in KV for {0}", userId);
                await _usersKeyvaultRepository.Upsert(userId, tokensResponse.RefreshToken, cancellationToken);
            }
            else
            {
                _logger.LogInformation("RefreshToken is empty for {0}", userId);
            }

            _logger.LogInformation("Execute GoogleFitClient.DataSourcesListRequest");
            var dataSourcesList = await _googleFitClient.DatasourcesListRequest(tokensResponse.AccessToken, cancellationToken);

            _logger.LogInformation("Create Eventhub Batch");

            // Get user's info for LastSync date
            _logger.LogInformation("Query userInfo");
            var user = await _usersTableRepository.GetById(userId, cancellationToken);

            // Generating datasetId based on event type
            DateTime startDateDt = DateTime.Now.AddDays(-30);
            DateTimeOffset startDateDto = new DateTimeOffset(startDateDt);
            if (user.LastSync != null)
            {
                startDateDto = user.LastSync.Value;
            }

            // Convert to DateTimeOffset to so .NET unix conversion is usable
            DateTimeOffset endDateDto = new DateTimeOffset(DateTime.Now);

            // .NET unix conversion only goes as small as milliseconds, multiplying to get nanoseconds
            var startDate = startDateDto.ToUnixTimeMilliseconds() * 1000000;
            var endDate = endDateDto.ToUnixTimeMilliseconds() * 1000000;
            var datasetId = startDate + "-" + endDate;

            List<Task> datasetRequestTasks = new List<Task>();

            // Add a task to get the Dataset for each dataSource
            foreach (var datasourceId in dataSourcesList.DatasourceIds)
            {
                datasetRequestTasks.Add(Task.Run(
                    async () =>
                    {
                        string pageToken = null;

                        // Make the Dataset requests, requesting the next page of data if necessary
                        do
                        {
                            _logger.LogInformation("Query Dataset: {0}", datasourceId);
                            var dataset = await _googleFitClient.DatasetRequest(
                                tokensResponse.AccessToken,
                                datasourceId,
                                datasetId,
                                cancellationToken,
                                pageToken);

                            // Save the NextPageToken
                            pageToken = dataset.NextPageToken;

                            // Add user id to payload
                            dataset.UserId = user.Id;

                            var jsonDataset = JsonConvert.SerializeObject(dataset);

                            _logger.LogInformation("Push Dataset: {0}", datasourceId);

                            // Create a batch of events for IoMT eventhub
                            using var eventBatch = await _eventHubProducerClient.CreateBatchAsync(cancellationToken);

                            // Push dataset to IoMT connector
                            if (!eventBatch.TryAdd(new EventData(jsonDataset)))
                            {
                                throw new Exception("Event is too large for the batch and cannot be sent.");
                            }

                            // Use the producer client to send the batch of events to the event hub
                            await _eventHubProducerClient.SendAsync(eventBatch, cancellationToken);
                            _logger.LogInformation("A batch of events has been published for {0}.", datasourceId);
                        }
                        while (pageToken != null);
                    }, cancellationToken));
            }

            // Wait for the Dataset request tasks to finish
            try
            {
                Task.WaitAll(datasetRequestTasks.ToArray(), cancellationToken);
            }
            catch (AggregateException ex)
            {
                foreach (var ie in ex.InnerExceptions)
                {
                    _logger.LogError("{0}: {1}", ie.GetType().Name, ie.Message);
                }
            }

            // Update LastSync column
            user.LastSync = endDateDto;
            await _usersTableRepository.Update(user, cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            await _eventHubProducerClient.DisposeAsync();
        }
    }
}
