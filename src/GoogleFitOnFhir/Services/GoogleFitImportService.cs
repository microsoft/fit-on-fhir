// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using EnsureThat;
using GoogleFitOnFhir.Clients.GoogleFit;
using GoogleFitOnFhir.Clients.GoogleFit.Config;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;
using GoogleFitOnFhir.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Common.Service;
using Microsoft.Health.Logging.Telemetry;
using Newtonsoft.Json;

namespace GoogleFitOnFhir.Services
{
    public class GoogleFitImportService : ParallelTaskWorker<GoogleFitImportOptions>, IAsyncDisposable
    {
        private readonly IGoogleFitClient _googleFitClient;
        private readonly EventHubProducerClient _eventHubProducerClient;
        private readonly ILogger<GoogleFitImportService> _logger;
        private readonly ITelemetryLogger _telemetryLogger;

        public GoogleFitImportService(
            IGoogleFitClient googleFitClient,
            EventHubProducerClient eventHubProducerClient,
            GoogleFitImportOptions options,
            ILogger<GoogleFitImportService> logger,
            ITelemetryLogger telemetryLogger)
            : base(options, options?.ParallelTaskOptions?.MaxConcurrency ?? 1)
        {
            _googleFitClient = EnsureArg.IsNotNull(googleFitClient, nameof(googleFitClient));
            _eventHubProducerClient = EnsureArg.IsNotNull(eventHubProducerClient, nameof(eventHubProducerClient));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
            _telemetryLogger = EnsureArg.IsNotNull(telemetryLogger, nameof(telemetryLogger));
        }

        public async Task ProcessDatasetRequests(
            User user,
            IEnumerable<string> dataSourceIds,
            string datasetId,
            AuthTokensResponse tokensResponse,
            CancellationToken cancellationToken)
        {
            var workItems = dataSourceIds.Select(
                datasourceId => new Func<Task>(
                async () =>
                {
                    string pageToken = null;

                    try
                    {
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

                            // Create a batch of events for IoMT eventhub
                            using var eventBatch = await _eventHubProducerClient.CreateBatchAsync(cancellationToken);
                            _logger.LogInformation("Create Eventhub Batch");

                            // Push dataset to IoMT connector
                            if (!eventBatch.TryAdd(new EventData(jsonDataset)))
                            {
                                throw new Exception("Event is too large for the batch and cannot be sent.");
                            }

                            // Use the producer client to send the batch of events to the event hub
                            await _eventHubProducerClient.SendAsync(eventBatch, cancellationToken);
                            _logger.LogInformation("Push Dataset: {0}", datasourceId);
                        }
                        while (pageToken != null);
                    }
                    catch (Exception ex)
                    {
                        if (!Options.ExceptionService.HandleException(ex, _telemetryLogger))
                        {
                            throw;
                        }
                    }
                }));

            // Wait for the Dataset request tasks to finish
            await StartWorker(workItems).ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            await _eventHubProducerClient.DisposeAsync();
        }
    }
}
