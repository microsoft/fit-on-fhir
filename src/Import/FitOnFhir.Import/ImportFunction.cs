// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Health.FitOnFhir.Common.Services;

namespace Microsoft.Health.FitOnFhir.Import
{
    public class ImportFunction
    {
        private readonly IImporterService _importerService;

        public ImportFunction(IImporterService importerService)
        {
            _importerService = EnsureArg.IsNotNull(importerService);
        }

        [FunctionName("import-data")]
        public async Task Run(
            [QueueTrigger("import-data")] string message,
            ILogger log,
            CancellationToken cancellationToken)
        {
            log.LogInformation("import-data has message: {0}", message);

            await _importerService.Import(message, cancellationToken);
        }
    }
}
