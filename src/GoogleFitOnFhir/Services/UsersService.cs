// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;
using GoogleFitOnFhir.Models;
using GoogleFitOnFhir.Repositories;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using GoogleFitClient = GoogleFitOnFhir.Clients.GoogleFit.GoogleFitClient;

namespace GoogleFitOnFhir.Services
{
    /// <summary>
    /// User Service.
    /// </summary>
    public class UsersService : IUsersService, IAsyncDisposable
    {
        private readonly IUsersTableRepository _usersTableRepository;
        private readonly GoogleFitClient _googleFitClient;
        private readonly EventHubProducerClient _eventHubProducerClient;
        private readonly ILogger<UsersService> _logger;
        private readonly IUsersKeyVaultRepository _usersKeyvaultRepository;
        private readonly IAuthService _authService;

        public UsersService(
            IUsersTableRepository usersTableRepository,
            GoogleFitClient googleFitClient,
            EventHubProducerClient eventHubProducerClient,
            IUsersKeyVaultRepository usersKeyvaultRepository,
            IAuthService authService,
            ILogger<UsersService> logger)
        {
            _usersTableRepository = usersTableRepository;
            _googleFitClient = googleFitClient;
            _eventHubProducerClient = eventHubProducerClient;
            _usersKeyvaultRepository = usersKeyvaultRepository;
            _authService = authService;
            _logger = logger;
        }

        public async Task<User> Initiate(string authCode, CancellationToken cancellationToken)
        {
            var tokenResponse = await _authService.AuthTokensRequest(authCode, cancellationToken);
            if (tokenResponse == null)
            {
                throw new Exception("Token response empty");
            }

            var emailResponse = await _googleFitClient.MyEmailRequest(tokenResponse.AccessToken, cancellationToken);
            if (emailResponse == null)
            {
                throw new Exception("Email response empty");
            }

            // https://developers.google.com/identity/protocols/oauth2/openid-connect#an-id-tokens-payload
            // Use the IdToken sub (Subject) claim for the user id - From the Google docs:
            // "An identifier for the user, unique among all Google accounts and never reused.
            // A Google account can have multiple email addresses at different points in time, but the sub value is never changed.
            // Use sub within your application as the unique-identifier key for the user.
            // Maximum length of 255 case-sensitive ASCII characters."
            string userId = tokenResponse.IdToken.Subject;
            User user = new User(userId);

            // Insert user into UsersTable
            await _usersTableRepository.Upsert(user, cancellationToken);

            // Insert refresh token into users KV by userId
            await _usersKeyvaultRepository.Upsert(userId, tokenResponse.RefreshToken, cancellationToken);

            return user;
        }

        public async Task ImportFitnessData(string userId, CancellationToken cancellationToken)
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
            AuthTokensResponse tokensResponse = await _authService.RefreshTokensRequest(refreshToken, cancellationToken);

            _logger.LogInformation("Execute GoogleFitClient.DataSourcesListRequest");

            // TODO: Store new refresh token
            var dataSourcesList = await _googleFitClient.DatasourcesListRequest(tokensResponse.AccessToken, cancellationToken);

            _logger.LogInformation("Create Eventhub Batch");

            // Create a batch of events for IoMT eventhub
            using EventDataBatch eventBatch = await _eventHubProducerClient.CreateBatchAsync(cancellationToken);

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

            // Get dataset for each dataSource
            foreach (var datasourceId in dataSourcesList.DatasourceIds)
            {
                _logger.LogInformation("Query Dataset: {0}", datasourceId);
                var dataset = await _googleFitClient.DatasetRequest(
                    tokensResponse.AccessToken,
                    datasourceId,
                    datasetId,
                    cancellationToken);

                // Add user id to payload
                dataset.UserId = user.Id;

                var jsonDataset = JsonConvert.SerializeObject(dataset);

                _logger.LogInformation("Push Dataset: {0}", datasourceId);

                // Push dataset to IoMT connector
                if (!eventBatch.TryAdd(new EventData(jsonDataset)))
                {
                    throw new Exception("Event is too large for the batch and cannot be sent.");
                }
            }

            // Use the producer client to send the batch of events to the event hub
            await _eventHubProducerClient.SendAsync(eventBatch, cancellationToken);
            _logger.LogInformation("A batch of events has been published.");

            // Update LastSync column
            user.LastSync = endDateDto;
            await _usersTableRepository.Update(user, cancellationToken);
        }

        public void QueueFitnessImport(User user)
        {
        }

        public async ValueTask DisposeAsync()
        {
            await _eventHubProducerClient.DisposeAsync();
        }
    }
}
