// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Health.FitOnFhir.Common;
using Microsoft.Health.FitOnFhir.Common.Services;

namespace Microsoft.Health.FitOnFhir.ImportTimerTrigger
{
    public class ImportTimerTriggerFunction
    {
        private readonly IImportTriggerMessageService _messageService;

        public ImportTimerTriggerFunction(IImportTriggerMessageService messageService)
        {
            _messageService = messageService;
        }

        [FunctionName("import-timer")]
        public async Task Run(
            [TimerTrigger("%SCHEDULE%")] TimerInfo myTimer,
            [Queue(Constants.QueueName, Connection = "AzureWebJobsStorage")] ICollector<string> queueService,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            await _messageService.AddImportMessagesToCollector(queueService, cancellationToken);
        }
    }
}
