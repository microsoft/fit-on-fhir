// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Storage.Queues;
using EnsureThat;
using Microsoft.Health.FitOnFhir.Common.Config;

namespace Microsoft.Health.FitOnFhir.Common.Providers
{
    public class QueueClientProvider : CredentialedProvider, IQueueClientProvider
    {
        private readonly Uri _queueServiceUri;

        public QueueClientProvider(AzureConfiguration configuration, ITokenCredentialProvider tokenCredentialProvider)
            : base(tokenCredentialProvider)
        {
            _queueServiceUri = EnsureArg.IsNotNull(configuration?.QueueServiceUri, nameof(configuration.QueueServiceUri));
        }

        public QueueClient GetQueueClient(string queueName)
        {
            EnsureArg.IsNotNullOrWhiteSpace(queueName, nameof(queueName));

            QueueUriBuilder uriBuilder = new QueueUriBuilder(_queueServiceUri)
            {
                QueueName = queueName,
            };

            QueueClientOptions options = new () { MessageEncoding = QueueMessageEncoding.Base64 };

            return new QueueClient(uriBuilder.ToUri(), GetTokenCredential(), options);
        }
    }
}
