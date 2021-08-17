using System;
using Azure.Messaging.EventHubs.Producer;
using GoogleFitOnFhir.Models;
using GoogleFitOnFhir.Persistence;
using GoogleFitOnFhir.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GoogleFitOnFhir.PublishData
{
    public class PublishData
    {
        private readonly IUsersService usersService;

        private readonly EventHubProducerClient producerClient;

        private readonly EventHubContext eventHubContext;

        private readonly ILogger<PublishData> log;

        public PublishData(
            IUsersService usersService,
            EventHubContext eventHubContext,
            ILogger<PublishData> log)
        {
            this.usersService = usersService;

            // this.producerClient = producerClient;

            // this.producerClient = new EventHubProducerClient(eventHubContext.ConnectionString);
            this.producerClient = new EventHubProducerClient(Environment.GetEnvironmentVariable("iomtConnectionString"));
            this.eventHubContext = eventHubContext;
            this.log = log;
        }

        [FunctionName("publish-data")]
        public void Run(
            [QueueTrigger("publish-data", Connection = "")] string myQueueItem)
        {
            this.log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");

            QueueMessageContext message = JsonConvert.DeserializeObject<QueueMessageContext>(myQueueItem);

            try
            {
                User user = new User(message.UserId);
                this.usersService.ImportFitnessData(user);
            }
            catch (Exception e)
            {
                this.log.LogError(e.Message);
            }
        }
    }
}
