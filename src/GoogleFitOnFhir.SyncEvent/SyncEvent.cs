using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GoogleFitOnFhir.Models;
using GoogleFitOnFhir.Repositories;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GoogleFitOnFhir.SyncEvent
{
    public class SyncEvent
    {
        private readonly IUsersTableRepository usersTableRepository;

        public SyncEvent(IUsersTableRepository usersTableRepository)
        {
            this.usersTableRepository = usersTableRepository;
        }

        [FunctionName("SyncEvent")]
        public void Run([TimerTrigger("0 0 * * * *")] TimerInfo myTimer, [Queue("publish-data", Connection = "AzureWebJobsStorage")] ICollector<string> queueService, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            IEnumerable<User> users = this.usersTableRepository.GetAll();

            log.LogInformation("{0} users in table", users.Count<User>());

            foreach (User user in users)
            {
                log.LogInformation("Adding {0} to queue", user.Id);
                queueService.Add(JsonConvert.SerializeObject(new QueueMessage
                {
                    UserId = user.Id,
                }));
            }
        }
    }
}
