using System;
using Azure.Messaging.EventHubs.Producer;
using GoogleFitOnFhir.Models;
using GoogleFitOnFhir.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace GoogleFitOnFhir.PublishData
{
    public class PublishData
    {
        private readonly IUsersService usersService;

        private readonly EventHubProducerClient producerClient;

        private readonly ILogger<PublishData> log;

        public PublishData(
            IUsersService usersService,
            EventHubProducerClient producerClient,
            ILogger<PublishData> log)
        {
            this.usersService = usersService;
            this.producerClient = producerClient;
            this.log = log;
        }

        [FunctionName("publish-data")]
        public void Run(
            [QueueTrigger("publish-data", Connection = "QueueConnectionString")] string myQueueItem)
        {
            this.log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");

            try
            {
                var user = new User("testUserId"); // TODO: Update this with the userID when we have it
                this.usersService.ImportFitnessData(user);
            }
            catch (Exception e)
            {
                this.log.LogError(e.Message);
            }
        }
    }
}
