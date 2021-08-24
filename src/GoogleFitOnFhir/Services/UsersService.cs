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
        private readonly IUsersTableRepository usersTableRepository;

        private readonly GoogleFitClient googleFitClient;

        private readonly EventHubProducerClient eventHubProducerClient;

        private readonly ILogger<UsersService> logger;

        private readonly IUsersKeyvaultRepository usersKeyvaultRepository;

        private readonly IAuthService authService;

        public UsersService(
            IUsersTableRepository usersTableRepository,
            GoogleFitClient googleFitClient,
            EventHubProducerClient eventHubProducerClient,
            IUsersKeyvaultRepository usersKeyvaultRepository,
            IAuthService authService,
            ILogger<UsersService> logger)
        {
            this.usersTableRepository = usersTableRepository;
            this.googleFitClient = googleFitClient;
            this.eventHubProducerClient = eventHubProducerClient;
            this.usersKeyvaultRepository = usersKeyvaultRepository;
            this.authService = authService;
            this.logger = logger;
        }

        public async Task<User> Initiate(string authCode)
        {
            var tokenResponse = await this.authService.AuthTokensRequest(authCode);
            if (tokenResponse == null)
            {
                throw new Exception("Token response empty");
            }

            var emailResponse = await this.googleFitClient.MyEmailRequest(tokenResponse.AccessToken);
            if (emailResponse == null)
            {
                throw new Exception("Email response empty");
            }

            // Hash email to reduce characterset for KV secret name compatability
            var userId = Utility.Base58String(emailResponse.EmailAddress);
            var user = new User(userId);

            // Insert user into UsersTable
            this.usersTableRepository.Upsert(user);

            // Insert refresh token into users KV by userId
            await this.usersKeyvaultRepository.Upsert(userId, tokenResponse.RefreshToken);

            return user;
        }

        public async Task ImportFitnessData(User user)
        {
            string refreshToken;

            this.logger.LogInformation("Get RefreshToken from KV for {0}", user.Id);

            try
            {
                refreshToken = await this.usersKeyvaultRepository.GetByName(user.Id);
            }
            catch (AggregateException ex)
            {
                this.logger.LogError(ex.Message);
                return;
            }

            this.logger.LogInformation("Refreshing the RefreshToken");
            AuthTokensResponse tokensResponse = await this.authService.RefreshTokensRequest(refreshToken);

            this.logger.LogInformation("Execute GoogleFitClient.DataSourcesListRequest");

            // TODO: Store new refresh token
            var dataSourcesList = await this.googleFitClient.DatasourcesListRequest(tokensResponse.AccessToken);

            this.logger.LogInformation("Create Eventhub Batch");

            // Create a batch of events for IoMT eventhub
            using EventDataBatch eventBatch = await this.eventHubProducerClient.CreateBatchAsync();

            // Get user's info for LastSync date
            this.logger.LogInformation("Query userInfo");
            var userInfo = this.usersTableRepository.GetById(user.Id);

            // Copy ETag over so we can successfully update the row when necessary
            user.ETag = userInfo.ETag;

            // Generating datasetId based on event type
            DateTime startDateDt = DateTime.Now.AddDays(-30);
            DateTimeOffset startDateDto = new DateTimeOffset(startDateDt);
            if (userInfo.LastSync != null)
            {
                startDateDto = userInfo.LastSync.Value;
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
                this.logger.LogInformation("Query Dataset: {0}", datasourceId);
                var dataset = await this.googleFitClient.DatasetRequest(
                    tokensResponse.AccessToken,
                    datasourceId,
                    datasetId);

                // Add user id to payload
                dataset.UserId = user.Id;

                var jsonDataset = JsonConvert.SerializeObject(dataset);

                this.logger.LogInformation("Push Dataset: {0}", datasourceId);

                // Push dataset to IoMT connector
                if (!eventBatch.TryAdd(new EventData(jsonDataset)))
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
                user.LastSync = endDateDto;
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
