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
    public class PublisherService : IPublisherService
    {
        private readonly IResponsibilityHandler<PublishRequest, Task> _handler;
        private readonly ILogger _logger;

        public PublisherService(IResponsibilityHandler<PublishRequest, Task> handler, ILogger<PublisherService> logger)
        {
            _handler = EnsureArg.IsNotNull(handler);
            _logger = EnsureArg.IsNotNull(logger);
        }

        /// <inheritdoc/>
        public Task PublishTo(QueueMessage message, CancellationToken cancellationToken)
        {
            try
            {
                var publishRequest = new PublishRequest() { Message = message, Token = cancellationToken };
                return _handler.Evaluate(publishRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Task.CompletedTask;
            }
        }
    }
}
