// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using FitOnFhir.Common.Models;
using FitOnFhir.Common.Repositories;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FitOnFhir.ImportTimerTrigger
{
    public class ImportTimerTriggerFunction
    {
        private readonly IUsersTableRepository _usersTableRepository;

        public ImportTimerTriggerFunction(IUsersTableRepository usersTableRepository)
        {
            _usersTableRepository = usersTableRepository;
        }

        [FunctionName("import-timer")]
        public async Task Run(
            [TimerTrigger("0 0 * * * *")] TimerInfo myTimer,
            [Queue("import-data", Connection = "AzureWebJobsStorage")] ICollector<string> queueService,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            AsyncPageable<User> usersPageable = _usersTableRepository.GetAll(cancellationToken);

            await foreach (User user in usersPageable)
            {
                logger.LogInformation("Adding {0} to queue", user.RowKey);
                foreach (var platformUserInfo in user.PlatformUserInfo)
                {
                    queueService.Add(JsonConvert.SerializeObject(new QueueMessage(user.RowKey, platformUserInfo.Value, platformUserInfo.Key)));
                }
            }
        }
    }
}
