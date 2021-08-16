using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace GoogleFitOnFhir.SyncEvent
{
    public static class SyncEvent
    {
        [FunctionName("SyncEvent")]
        public static void Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            // TODO: remove once using query in PR #62 - https://github.com/microsoft/googlefit-on-fhir/pull/62
            string[,] usersArray = { { "A801C48320EC6E9A47EA2B844C9C7CC6", "2021-08-13T19:43:34.229Z" } };
        }

        [FunctionName("QueueOutput")]
        [return: Queue("myqueue-items")]
        public static string QueueOutput([HttpTrigger] dynamic input,  ILogger log)
        {
            log.LogInformation($"C# function processed: {input.Text}");
            return input.Text;
        }
    }
}
