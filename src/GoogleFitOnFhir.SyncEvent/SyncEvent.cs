using System;
using System.Collections;
using GoogleFitOnFhir.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace GoogleFitOnFhir.SyncEvent
{
    [StorageAccount("AzureWebJobsStorage")]
    public class SyncEvent
    {
        [FunctionName("SyncEvent")]
        [return: Queue("publish-data")]
        public static string Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, ICollector<string> queueService, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            log.LogInformation($"C# Queue trigger function processed: {queueService}");

            // TODO: remove once using query in PR #62 - https://github.com/microsoft/googlefit-on-fhir/pull/62
            string[] usersArray = { "A801C48320EC6E9A47EA2B844C9C7CC6", "A801C48320EC6E9A47EA2B844C9C7CC6" };

            foreach (string user in usersArray)
            {
                Console.WriteLine($"{user} ");
                var message = new QueueMessage();
                message.UserId = user;
                queueService.Add(message + "(step 1)");
            }

            return string.Empty;
        }
    }
}
