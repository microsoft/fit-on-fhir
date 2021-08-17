using System;
using System.Collections;
using GoogleFitOnFhir.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace GoogleFitOnFhir.SyncEvent
{
    public class SyncEvent
    {
        [FunctionName("SyncEvent")]
        public static void Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, [Queue("publish-data", Connection = "AzureWebJobsStorage")] ICollector<string> queueService, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            // TODO: remove once using query in PR #62 - https://github.com/microsoft/googlefit-on-fhir/pull/62
            string[] usersArray = { "A801C48320EC6E9A47EA2B844C9C7CC6", "A801C48320EC6E9A47EA2B844C9C7CC6" };

            // foreach (string userId in usersArray)
            // {
            //     Console.WriteLine($"{userId} ");
            var message = new QueueMessage();
            message.UserId = usersArray[0];
            queueService.Add(message + "(step 1)");
            Console.WriteLine(message.UserId);

            // }
            // return string.Empty;
        }
    }
}
