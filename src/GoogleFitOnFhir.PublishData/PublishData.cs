// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using GoogleFitOnFhir.Models;
using GoogleFitOnFhir.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GoogleFitOnFhir.PublishData
{
    public class PublishData
    {
        private readonly IDataImporterService _dataImporterService;

        public PublishData(IDataImporterService dataImporterService)
        {
            _dataImporterService = EnsureArg.IsNotNull(dataImporterService);
        }

        [FunctionName("publish-data")]
        public async Task Run(
            [QueueTrigger("publish-data")] string myQueueItem,
            ILogger log,
            CancellationToken cancellationToken)
        {
            log.LogInformation("publish-data has message: {0}", myQueueItem);
            QueueMessage message = JsonConvert.DeserializeObject<QueueMessage>(myQueueItem);

            await _dataImporterService.ImportFrom(message, cancellationToken);
        }
    }
}
