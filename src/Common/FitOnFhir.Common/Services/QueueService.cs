// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Storage.Queues;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.FitOnFhir.Common.Interfaces;
using Microsoft.Health.FitOnFhir.Common.Models;
using Microsoft.Health.FitOnFhir.Common.Providers;
using Newtonsoft.Json;

namespace Microsoft.Health.FitOnFhir.Common.Services
{
    public class QueueService : IQueueService
    {
        private readonly ILogger<QueueService> _logger;
        private readonly QueueClient _queueClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueService"/> class.
        /// </summary>
        /// <param name="queueClientProvider">an instance of <see cref="IQueueClientProvider"/></param>
        /// <param name="logger">An instance of a logger for this class.</param>
        public QueueService(IQueueClientProvider queueClientProvider, ILogger<QueueService> logger)
        {
            _queueClient = EnsureArg.IsNotNull(queueClientProvider, nameof(queueClientProvider)).GetQueueClient(Constants.ImportDataQueueName);
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        /// <inheritdoc/>
        public async Task SendQueueMessage(string userId, string platformUserId, string platformName, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Adding user [{0}] to queue [{1}] for platform [{2}]", userId, Constants.ImportDataQueueName, platformName);
            var queueMessage = new QueueMessage(userId, platformUserId, platformName);
            var response = await _queueClient.SendMessageAsync(JsonConvert.SerializeObject(queueMessage), cancellationToken);
            var rawResponse = response.GetRawResponse();
            _logger.LogDebug("Response from message send: status '{0}', reason'{1}'", rawResponse.Status, rawResponse.ReasonPhrase);
        }
    }
}
