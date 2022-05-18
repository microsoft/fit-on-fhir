// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Producer;
using EnsureThat;
using FitOnFhir.Common.Exceptions;
using FitOnFhir.Common.Models;
using FitOnFhir.GoogleFit.Clients.GoogleFit;
using FitOnFhir.GoogleFit.Clients.GoogleFit.Config;
using FitOnFhir.GoogleFit.Clients.GoogleFit.Models;
using FitOnFhir.GoogleFit.Clients.GoogleFit.Responses;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Common.Service;
using Microsoft.Health.Logging.Telemetry;

namespace FitOnFhir.GoogleFit.Services
{
    public class GoogleFitImportService : ParallelTaskWorker<GoogleFitImportOptions>, IGoogleFitImportService, IAsyncDisposable
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

        /// <inheritdoc/>
        public async Task ProcessDatasetRequests(
            User user,
            IEnumerable<DataSource> dataSources,
            string datasetId,
            AuthTokensResponse tokensResponse,
            CancellationToken cancellationToken)
        {
            var workItems = dataSources.Select(
                dataSource => new Func<Task>(
                async () =>
                {
                    var dataStreamId = dataSource.DataStreamId;
                    string pageToken = null;

                    try
                    {
                        // Make the Dataset requests, requesting the next page of data if necessary
                        do
                        {
                            _logger.LogInformation("Query Dataset: {0}", dataStreamId);
                            var medTechDataset = await _googleFitClient.DatasetRequest(
                                tokensResponse.AccessToken,
                                dataSource,
                                datasetId,
                                cancellationToken,
                                pageToken);

                            if (medTechDataset == null)
                            {
                                _logger.LogInformation("No Dataset for: {0}", dataStreamId);
                                continue;
                            }

                            // Save the NextPageToken
                            pageToken = medTechDataset.GetDataset().NextPageToken;

                            // Create a batch of events for MedTech Service
                            using var eventBatch = await _eventHubProducerClient.CreateBatchAsync(cancellationToken);
                            _logger.LogInformation("Created Eventhub Batch (size {0}, count {1})", eventBatch.SizeInBytes, eventBatch.Count);

                            _logger.LogInformation("Push Dataset: {0}", dataStreamId);

                            // Push dataset to MedTech Service
                            if (!eventBatch.TryAdd(medTechDataset.ToEventData(user.Id)))
                            {
                                var eventBatchException = new EventBatchException("Event is too large for the batch and cannot be sent.");
                                _logger.LogError(eventBatchException, eventBatchException.Message);
                            }

                            // Use the producer client to send the batch of events to the event hub
                            await _eventHubProducerClient.SendAsync(eventBatch, cancellationToken);
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
            await StartWorker(workItems);
        }

        public async ValueTask DisposeAsync()
        {
            await _eventHubProducerClient.DisposeAsync();
        }
    }
}
