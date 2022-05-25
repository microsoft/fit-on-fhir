// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Messaging.EventHubs.Producer;
using EnsureThat;
using FitOnFhir.Common.Exceptions;
using FitOnFhir.GoogleFit.Client;
using FitOnFhir.GoogleFit.Client.Config;
using FitOnFhir.GoogleFit.Client.Models;
using FitOnFhir.GoogleFit.Client.Responses;
using FitOnFhir.GoogleFit.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Common.Service;
using Microsoft.Health.Logging.Telemetry;

namespace FitOnFhir.GoogleFit.Services
{
    public class GoogleFitImportService : ParallelTaskWorker<GoogleFitImportOptions>, IGoogleFitImportService, IAsyncDisposable
    {
        private readonly IGoogleFitClient _googleFitClient;
        private readonly IGoogleFitUserRepository _googleFitUserRepository;
        private readonly EventHubProducerClient _eventHubProducerClient;
        private readonly Func<DateTimeOffset> _utcNowFunc;
        private readonly ILogger<GoogleFitImportService> _logger;
        private readonly ITelemetryLogger _telemetryLogger;

        public GoogleFitImportService(
            IGoogleFitClient googleFitClient,
            IGoogleFitUserRepository googleFitUserRepository,
            EventHubProducerClient eventHubProducerClient,
            GoogleFitImportOptions options,
            Func<DateTimeOffset> utcNowFunc,
            ILogger<GoogleFitImportService> logger,
            ITelemetryLogger telemetryLogger)
            : base(options, options?.ParallelTaskOptions?.MaxConcurrency ?? 1)
        {
            _googleFitClient = EnsureArg.IsNotNull(googleFitClient, nameof(googleFitClient));
            _googleFitUserRepository = googleFitUserRepository;
            _eventHubProducerClient = EnsureArg.IsNotNull(eventHubProducerClient, nameof(eventHubProducerClient));
            _utcNowFunc = utcNowFunc;
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
            _telemetryLogger = EnsureArg.IsNotNull(telemetryLogger, nameof(telemetryLogger));
        }

        /// <inheritdoc/>
        public async Task ProcessDatasetRequests(string userId, IEnumerable<DataSource> dataSources, AuthTokensResponse tokensResponse, CancellationToken cancellationToken)
        {
            // Get user's info for LastSync date
             _logger.LogInformation("Query userInfo");
             var user = await _googleFitUserRepository.GetById(userId, cancellationToken);

             var workItems = dataSources.Select(
                dataSource => new Func<Task>(
                async () =>
                {
                    try
                    {
                        var dataStreamId = dataSource.DataStreamId;
                        string pageToken = null;
                        string datasetId;
                        DateTimeOffset currentTime;

                        if (user.SyncTimes.TryGetValue(dataStreamId, out var lastSyncTime))
                        {
                            datasetId = GenerateDataSetId(lastSyncTime, out currentTime);
                        }
                        else
                        {
                            datasetId = GenerateDataSetId(null, out currentTime);
                        }

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
                            // TODO should this be the GoogleFitUser ID or the top level Users partition ID?
                            if (!eventBatch.TryAdd(medTechDataset.ToEventData(userId)))
                            {
                                var eventBatchException = new EventBatchException("Event is too large for the batch and cannot be sent.");
                                _logger.LogError(eventBatchException, eventBatchException.Message);
                            }

                            // Use the producer client to send the batch of events to the event hub
                            await _eventHubProducerClient.SendAsync(eventBatch, cancellationToken);
                        }
                        while (pageToken != null);

                        // Update the GoogleFitUser timestamp for this data source
                        user.SyncTimes.Remove(dataStreamId);
                        user.SyncTimes.Add(dataStreamId, currentTime);
                        await _googleFitUserRepository.Update(user, cancellationToken);
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

        private string GenerateDataSetId(DateTimeOffset? lastSyncTime, out DateTimeOffset currentTime)
        {
            // if this DataSource has never been synced before, then retrieve 30 days prior worth of data
            DateTimeOffset startDateDto = _utcNowFunc().AddDays(-30);
            if (lastSyncTime != null)
            {
                startDateDto = lastSyncTime.Value;
            }

            // Convert to DateTimeOffset to so .NET unix conversion is usable
            currentTime = _utcNowFunc();

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
