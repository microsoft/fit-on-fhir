// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Azure.Data.Tables;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Health.FitOnFhir.Common.Models;
using Microsoft.Health.FitOnFhir.Common.Repositories;
using Microsoft.Health.FitOnFhir.Common.Resolvers;
using Newtonsoft.Json;

namespace Microsoft.Health.FitOnFhir.Common.Services
{
    public class ImportTriggerMessageService : IImportTriggerMessageService
    {
        private readonly IUsersTableRepository _usersTableRepository;
        private readonly ILogger<ImportTriggerMessageService> _logger;

        public ImportTriggerMessageService(IUsersTableRepository usersTableRepository, ILogger<ImportTriggerMessageService> logger)
        {
            _usersTableRepository = EnsureArg.IsNotNull(usersTableRepository, nameof(usersTableRepository));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        public async Task AddImportMessagesToCollector(ICollector<string> collector, CancellationToken cancellationToken)
        {
            AsyncPageable<TableEntity> tableEntities = _usersTableRepository.GetAll(cancellationToken);

            await foreach (TableEntity entity in tableEntities)
            {
                User user = new User(entity);

                _logger.LogInformation("Adding {0} to queue", user.Id);
                IEnumerable<PlatformUserInfo> userPlatformInformation = user.GetPlatformUserInfo().Where(upi => upi.ImportState == DataImportState.ReadyToImport);
                foreach (var userPlatformInfo in userPlatformInformation)
                {
                    user.UpdateImportState(userPlatformInfo.PlatformName, DataImportState.Queued);
                    user = await _usersTableRepository.Update(
                        user,
                        UserConflictResolvers.ResolveConflictDefault,
                        cancellationToken);
                    collector.Add(JsonConvert.SerializeObject(new QueueMessage(user.Id, userPlatformInfo.UserId, userPlatformInfo.PlatformName)));
                }
            }
        }
    }
}
