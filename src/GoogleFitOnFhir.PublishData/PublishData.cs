using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using GoogleFitOnFhir.Clients.GoogleFit.Models;
using GoogleFitOnFhir.Models;
using GoogleFitOnFhir.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GoogleFitOnFhir.PublishData
{
    public static class PublishData
    {
        [FunctionName("publish-data")]
        public static async Task Run(
            [QueueTrigger("publish-data", Connection = "QueueConnectionString")] string myQueueItem,
            ILogger log,
            IUsersService usersService,
            EventHubProducerClient producerClient)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");

            try
            {
                var user = new User("testUserId"); // TODO: Update this with the userID when we have it
                usersService.ImportFitnessData(user);
            }
            catch (Exception e)
            {
                log.LogError(e.Message);
            }
        }
    }
}
