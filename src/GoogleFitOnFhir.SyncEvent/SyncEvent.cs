using System;
using System.Collections;
using System.Collections.Generic;
using GoogleFitOnFhir.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace GoogleFitOnFhir.SyncEvent
{
    public class SyncEvent
    {
        [FunctionName("SyncEvent")]
        public static void Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer, [Queue("publish-data", Connection = "AzureWebJobsStorage")] ICollector<string> queueService, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            // TODO: Use GetAll() instead of hardcoded array (https://github.com/microsoft/googlefit-on-fhir/pull/62/files#diff-12ef371094ae0c16b4de28ded773034d72e48b3e54c11409ad9be5dbda4aa873R28)
            var users = new List<string>()
                { "A801C48320EC6E9A47EA2B844C9C7CC6", "DSFKJ39234LASKDFJNL349SDLFKSDF" };

            users.ForEach(userId =>
            {
                var message = new QueueMessage
                {
                    UserId = userId,
                };
                queueService.Add(message.ToString());
            });
        }
    }
}
