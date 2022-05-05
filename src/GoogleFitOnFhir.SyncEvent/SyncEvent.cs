// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using GoogleFitOnFhir.Clients.GoogleFit.Handlers;
using GoogleFitOnFhir.Models;
using GoogleFitOnFhir.Repositories;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GoogleFitOnFhir.SyncEvent
{
    public class SyncEvent
    {
        private readonly IUsersTableRepository _usersTableRepository;

        public SyncEvent(IUsersTableRepository usersTableRepository)
        {
            _usersTableRepository = usersTableRepository;
        }

        [FunctionName("SyncEvent")]
        public async Task Run(
            [TimerTrigger("0 0 * * * *")] TimerInfo myTimer,
            [Queue("publish-data", Connection = "AzureWebJobsStorage")] ICollector<string> queueService,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            AsyncPageable<User> usersPageable = _usersTableRepository.GetAll(cancellationToken);

            await foreach (User user in usersPageable)
            {
                logger.LogInformation("Adding {0} to queue", user.Id);
                queueService.Add(JsonConvert.SerializeObject(new QueueMessage
                {
                    UserId = user.Id,
                    PlatformName = GoogleFitDataImportHandler.GoogleFitPlatform,
                }));
            }
        }
    }
}
