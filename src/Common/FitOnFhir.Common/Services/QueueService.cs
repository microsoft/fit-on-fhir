// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Storage.Queues;
using EnsureThat;
using FitOnFhir.Common.Config;
using FitOnFhir.Common.Interfaces;
using FitOnFhir.Common.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FitOnFhir.Common.Services
{
    public class QueueService : IQueueService
    {
        private readonly ILogger<QueueService> _logger;
        private readonly QueueClient _queueClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueService"/> class.
        /// Creates a <see cref="QueueClient"/> from the <see cref="AzureConfiguration"/>.StorageAccountConnectionString and <see cref="Constants"/>.QueueName
        /// </summary>
        /// <param name="azureConfiguration">The <see cref="AzureConfiguration"/>
        /// which contains the environment variable value for the storage account connection string.</param>
        /// <param name="logger">An instance of a logger for this class.</param>
        public QueueService(AzureConfiguration azureConfiguration, ILogger<QueueService> logger)
        {
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
            QueueClientOptions queueOptions = new () { MessageEncoding = QueueMessageEncoding.Base64 };
            _queueClient = new QueueClient(azureConfiguration.StorageAccountConnectionString, Constants.QueueName, queueOptions);
            _ = InitQueue();
        }

        /// <inheritdoc/>
        public async Task SendQueueMessage(string userId, string platformUserId, string platformName)
        {
            _logger.LogInformation("Adding user [{0}] to queue [{1}] for platform [{2}]", userId, Constants.QueueName, platformName);
            var queueMessage = new QueueMessage(userId, platformUserId, platformName);
            var response = await _queueClient.SendMessageAsync(JsonConvert.SerializeObject(queueMessage));
            _logger.LogDebug("Response from message send {0}", response.Value);
        }

        private async Task InitQueue()
        {
            if (await _queueClient.CreateIfNotExistsAsync() != null)
            {
                _logger.LogInformation("Queue {0} created", Constants.QueueName);
            }
        }
    }
}
