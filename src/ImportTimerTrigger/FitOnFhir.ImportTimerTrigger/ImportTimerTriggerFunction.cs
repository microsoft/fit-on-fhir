// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
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
            [TimerTrigger("0 */1 * * * *")] TimerInfo myTimer,
            [Queue("import-data", Connection = "AzureWebJobsStorage")] ICollector<string> queueService,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            AsyncPageable<TableEntity> tableEntities = _usersTableRepository.GetAll(cancellationToken);

            await foreach (TableEntity entity in tableEntities)
            {
                User user = new User(entity);

                logger.LogInformation("Adding {0} to queue", user.Id);
                IEnumerable<PlatformUserInfo> userPlatformInformation = user.GetPlatformUserInfo();
                foreach (var userPlatformInfo in userPlatformInformation)
                {
                    queueService.Add(JsonConvert.SerializeObject(new QueueMessage(user.Id, userPlatformInfo.UserId, userPlatformInfo.PlatformName)));
                }
            }
        }
    }
}
