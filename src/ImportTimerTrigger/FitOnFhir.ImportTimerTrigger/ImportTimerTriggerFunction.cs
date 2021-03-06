// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Health.FitOnFhir.Common;
using Microsoft.Health.FitOnFhir.Common.Models;
using Microsoft.Health.FitOnFhir.Common.Repositories;
using Microsoft.Health.FitOnFhir.Common.Resolvers;
using Newtonsoft.Json;

namespace Microsoft.Health.FitOnFhir.ImportTimerTrigger
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
            [TimerTrigger("%SCHEDULE%")] TimerInfo myTimer,
            [Queue(Constants.QueueName, Connection = "AzureWebJobsStorage")] ICollector<string> queueService,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            AsyncPageable<TableEntity> tableEntities = _usersTableRepository.GetAll(cancellationToken);

            await foreach (TableEntity entity in tableEntities)
            {
                User user = new User(entity);

                logger.LogInformation("Adding {0} to queue", user.Id);
                IEnumerable<PlatformUserInfo> userPlatformInformation = user.GetPlatformUserInfo().Where(upi => upi.ImportState == DataImportState.ReadyToImport);
                foreach (var userPlatformInfo in userPlatformInformation)
                {
                    user.UpdateImportState(userPlatformInfo.PlatformName, DataImportState.Queued);
                    user = await _usersTableRepository.Update(
                        user,
                        UserConflictResolvers.ResolveConflictDefault,
                        cancellationToken);
                    queueService.Add(JsonConvert.SerializeObject(new QueueMessage(user.Id, userPlatformInfo.UserId, userPlatformInfo.PlatformName)));
                }
            }
        }
    }
}
