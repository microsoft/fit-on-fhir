// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using GoogleFitOnFhir.Common;
using GoogleFitOnFhir.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Common.Handler;

namespace GoogleFitOnFhir.Services
{
    public class ImporterService : IImporterService
    {
        private readonly IResponsibilityHandler<ImportRequest, Task> _handler;
        private readonly ILogger _logger;

        public ImporterService(IResponsibilityHandler<ImportRequest, Task> handler, ILogger<ImporterService> logger)
        {
            _handler = EnsureArg.IsNotNull(handler);
            _logger = EnsureArg.IsNotNull(logger);
        }

        /// <inheritdoc/>
        public Task Import(QueueMessage message, CancellationToken cancellationToken)
        {
            try
            {
                var importRequest = new ImportRequest() { Message = message, Token = cancellationToken };
                return _handler.Evaluate(importRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Task.FromException(ex);
            }
        }
    }
}
