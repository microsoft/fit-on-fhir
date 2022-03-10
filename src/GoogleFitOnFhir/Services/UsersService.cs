// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;
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
        private readonly IUsersTableRepository _usersTableRepository;
        private readonly GoogleFitClient _googleFitClient;
        private readonly EventHubProducerClient _eventHubProducerClient;
        private readonly ILogger<UsersService> _logger;
        private readonly IUsersKeyvaultRepository _usersKeyvaultRepository;
        private readonly IAuthService _authService;

        public UsersService(
            IUsersTableRepository usersTableRepository,
            GoogleFitClient googleFitClient,
            EventHubProducerClient eventHubProducerClient,
            IUsersKeyvaultRepository usersKeyvaultRepository,
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

        public async Task<User> Initiate(string authCode)
        {
            var tokenResponse = await _authService.AuthTokensRequest(authCode);
            if (tokenResponse == null)
            {
                throw new Exception("Token response empty");
            }

            var emailResponse = await _googleFitClient.MyEmailRequest(tokenResponse.AccessToken);
            if (emailResponse == null)
            {
                throw new Exception("Email response empty");
            }

            // Hash email to reduce characterset for KV secret name compatability
            var userId = Utility.Base58String(emailResponse.EmailAddress);
            var user = new User(userId);

            // Insert user into UsersTable
            _usersTableRepository.Upsert(user);

            // Insert refresh token into users KV by userId
            await _usersKeyvaultRepository.Upsert(userId, tokenResponse.RefreshToken);

            return user;
        }

        public async Task ImportFitnessData(string userId)
        {
            string refreshToken;

            _logger.LogInformation("Get RefreshToken from KV for {0}", userId);

            try
            {
                refreshToken = await _usersKeyvaultRepository.GetByName(userId);
            }
            catch (AggregateException ex)
            {
                _logger.LogError(ex.Message);
                return;
            }

            _logger.LogInformation("Refreshing the RefreshToken");
            AuthTokensResponse tokensResponse = await _authService.RefreshTokensRequest(refreshToken);

            _logger.LogInformation("Execute GoogleFitClient.DataSourcesListRequest");

            // TODO: Store new refresh token
            var dataSourcesList = await _googleFitClient.DatasourcesListRequest(tokensResponse.AccessToken);

            _logger.LogInformation("Create Eventhub Batch");

            // Create a batch of events for IoMT eventhub
            using EventDataBatch eventBatch = await _eventHubProducerClient.CreateBatchAsync();

            // Get user's info for LastSync date
            _logger.LogInformation("Query userInfo");
            var user = _usersTableRepository.GetById(userId);

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
                    datasetId);

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

            try
            {
                // Use the producer client to send the batch of events to the event hub
                await _eventHubProducerClient.SendAsync(eventBatch);
                _logger.LogInformation("A batch of events has been published.");

                // Update LastSync column
                user.LastSync = endDateDto;
                _usersTableRepository.Update(user);
            }
            finally
            {
                await _eventHubProducerClient.DisposeAsync();
            }
        }

        public void QueueFitnessImport(User user)
        {
        }
    }
}
