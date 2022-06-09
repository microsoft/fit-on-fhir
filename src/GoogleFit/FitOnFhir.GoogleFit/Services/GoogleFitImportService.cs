// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Messaging.EventHubs.Producer;
using EnsureThat;
using FitOnFhir.Common.Requests;
using FitOnFhir.GoogleFit.Client;
using FitOnFhir.GoogleFit.Client.Config;
using FitOnFhir.GoogleFit.Client.Models;
using FitOnFhir.GoogleFit.Client.Responses;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Common.Service;
using Microsoft.Health.Logging.Telemetry;

namespace FitOnFhir.GoogleFit.Services
{
    public class GoogleFitImportService : ParallelTaskWorker<GoogleFitImportOptions>, IGoogleFitImportService, IAsyncDisposable
    {
        private readonly IGoogleFitClient _googleFitClient;
        private readonly EventHubProducerClient _eventHubProducerClient;
        private readonly Func<DateTimeOffset> _utcNowFunc;
        private readonly ILogger<GoogleFitImportService> _logger;
        private readonly ITelemetryLogger _telemetryLogger;

        public GoogleFitImportService(
            IGoogleFitClient googleFitClient,
            EventHubProducerClient eventHubProducerClient,
            GoogleFitImportOptions options,
            Func<DateTimeOffset> utcNowFunc,
            ILogger<GoogleFitImportService> logger,
            ITelemetryLogger telemetryLogger)
            : base(options, options?.ParallelTaskOptions?.MaxConcurrency ?? 1)
        {
            _googleFitClient = EnsureArg.IsNotNull(googleFitClient, nameof(googleFitClient));
            _eventHubProducerClient = EnsureArg.IsNotNull(eventHubProducerClient, nameof(eventHubProducerClient));
            _utcNowFunc = utcNowFunc;
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
            _telemetryLogger = EnsureArg.IsNotNull(telemetryLogger, nameof(telemetryLogger));
        }

        private RequestLimiter Limiter { get; set; }

        /// <inheritdoc/>
        public async Task ProcessDatasetRequests(
            GoogleFitUser user,
            IEnumerable<DataSource> dataSources,
            AuthTokensResponse tokensResponse,
            CancellationToken cancellationToken)
        {
            Limiter = new RequestLimiter(Options.MaximumRequestsPerMinute, _utcNowFunc);

            var workItems = dataSources.Select(
                dataSource => new Func<Task>(
                async () =>
                {
                    try
                    {
                        var dataStreamId = dataSource.DataStreamId;
                        string pageToken = null;
                        string datasetId;

                        // Get the time this data stream was synced from the GoogleFitUser object
                        if (user.TryGetLastSyncTime(dataStreamId, out var lastSyncTime))
                        {
                            datasetId = GenerateDataSetId(lastSyncTime);
                        }
                        else
                        {
                            datasetId = GenerateDataSetId(default);
                        }

                        // Make the Dataset requests, requesting the next page of data if necessary
                        do
                        {
                            // Calculate if a delay needs to be added after a request is made.
                            // Requests may need to be throttled to avoid 429 responses from Google.
                            if (Limiter.TryThrottle(cancellationToken, out Task delayTask, out double delayMs))
                            {
                                // When large amounts of data are processd we may need to throttle requests to prevent exceeding the API rate limits.
                                _logger.LogInformation("Throttling request for Dataset: {0} for user: {1}. Delay ms: {2}", dataStreamId, user.Id, delayMs);
                                await delayTask;
                            }

                            _logger.LogInformation("Query Dataset: {0} for user: {1}", dataStreamId, user.Id);
                            var medTechDataset = await _googleFitClient.DatasetRequest(
                                tokensResponse.AccessToken,
                                dataSource,
                                datasetId,
                                Options.DataPointPageLimit,
                                cancellationToken,
                                pageToken);

                            if (medTechDataset == null)
                            {
                                _logger.LogInformation("No Dataset for: {0} for user: {1}", dataStreamId, user.Id);
                                break;
                            }

                            // Save the NextPageToken
                            pageToken = medTechDataset.GetPageToken();

                            // Create a batch of events for MedTech Service
                            using var eventBatch = await _eventHubProducerClient.CreateBatchAsync(cancellationToken);

                            _logger.LogInformation("Push Dataset: {0} for user: {1}", dataStreamId, user.Id);

                            // Push dataset to MedTech Service
                            if (!eventBatch.TryAdd(medTechDataset.ToEventData(user.Id)))
                            {
                                _logger.LogError("Event data too large, Dataset: {1}, User: {2}", eventBatch.SizeInBytes, dataStreamId, user.Id);
                            }

                            _logger.LogInformation("EventHub Batch (actual bytes {0}, maximum bytes {1}), Dataset: {2}, User: {3}", eventBatch.SizeInBytes, eventBatch.MaximumSizeInBytes, dataStreamId, user.Id);

                            // Use the producer client to send the batch of events to the event hub
                            await _eventHubProducerClient.SendAsync(eventBatch, cancellationToken);

                            // Calculate the latest start time in the data set.
                            // There might be a delay in when data arrives to the Google service,
                            // so always sync from the last known start date of data.
                            DateTimeOffset lastStartTime = medTechDataset.GetMaxStartTime();

                            if (lastStartTime != default)
                            {
                                // Update the last sync time for this DataSource in the GoogleFitUser
                                _logger.LogInformation("Saving last sync time for Dataset: {0} for user: {1}", dataStreamId, user.Id);
                                user.SaveLastSyncTime(dataStreamId, lastStartTime);
                            }
                            else
                            {
                                _logger.LogWarning("No latest start date found for Dataset: {0} for user: {1}", dataStreamId, user.Id);
                            }
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

        private string GenerateDataSetId(DateTimeOffset lastSyncTime)
        {
            // if this DataSource has never been synced before, then retrieve 30 days prior worth of data
            DateTimeOffset startDateDto = _utcNowFunc().AddDays(-30);
            if (lastSyncTime != default)
            {
                startDateDto = lastSyncTime;
            }

            // Convert to DateTimeOffset to so .NET unix conversion is usable
            DateTimeOffset currentTime = _utcNowFunc();

            // .NET unix conversion only goes as small as milliseconds, multiplying to get nanoseconds
            var startDate = startDateDto.ToUnixTimeMilliseconds() * 1000000;
            var endDate = currentTime.ToUnixTimeMilliseconds() * 1000000;
            return startDate + "-" + endDate;
        }

        public async ValueTask DisposeAsync()
        {
            await _eventHubProducerClient.DisposeAsync();
        }
    }
}
