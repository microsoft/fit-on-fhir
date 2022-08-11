// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Common.Handler;
using Microsoft.Health.FitOnFhir.Common.Models;
using Microsoft.Health.FitOnFhir.Common.Requests;
using Newtonsoft.Json;

namespace Microsoft.Health.FitOnFhir.Common.Services
{
    public class ImporterService : IImporterService
    {
        private readonly IResponsibilityHandler<ImportRequest, Task<bool?>> _handler;
        private readonly ILogger _logger;

        public ImporterService(IResponsibilityHandler<ImportRequest, Task<bool?>> handler, ILogger<ImporterService> logger)
        {
            _handler = EnsureArg.IsNotNull(handler);
            _logger = EnsureArg.IsNotNull(logger);
        }

        /// <inheritdoc/>
        public Task Import(string message, CancellationToken cancellationToken)
        {
            try
            {
                QueueMessage queueMessage = JsonConvert.DeserializeObject<QueueMessage>(message);
                var importRequest = new ImportRequest(queueMessage, cancellationToken);
                return _handler.Evaluate(importRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Task.FromException(ex);
            }
        }
    }
}
