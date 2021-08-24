using System;
using System.Threading.Tasks;
using GoogleFitOnFhir.Models;
using GoogleFitOnFhir.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GoogleFitOnFhir.PublishData
{
    public class PublishData
    {
        private readonly IUsersService usersService;
        private readonly ILogger<PublishData> log;

        public PublishData(
            IUsersService usersService,
            ILogger<PublishData> log)
        {
            this.usersService = usersService;
            this.log = log;
        }

        [FunctionName("publish-data")]
        public async Task Run(
            [QueueTrigger("publish-data")] string myQueueItem)
        {
            this.log.LogInformation("publish-data has message: {0}", myQueueItem);
            QueueMessage message = JsonConvert.DeserializeObject<QueueMessage>(myQueueItem);
            await this.usersService.ImportFitnessData(message.UserId);
        }
    }
}
