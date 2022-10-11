// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Messaging.EventHubs.Producer;
using EnsureThat;
using Microsoft.Health.FitOnFhir.Common.Config;

namespace Microsoft.Health.FitOnFhir.Common.Providers
{
    public class EventHubProducerClientProvider : CredentialedProvider, IEventHubProducerClientProvider
    {
        private readonly string _eventHubNamespace;
        private readonly string _eventHubName;

        public EventHubProducerClientProvider(AzureConfiguration azureConfiguration, ITokenCredentialProvider tokenCredentialProvider)
            : base(tokenCredentialProvider)
        {
            _eventHubNamespace = EnsureArg.IsNotNullOrWhiteSpace(azureConfiguration?.EventHubFullyQualifiedNamespace, nameof(azureConfiguration.EventHubFullyQualifiedNamespace));
            _eventHubName = EnsureArg.IsNotNullOrWhiteSpace(azureConfiguration?.EventHubName, nameof(azureConfiguration.EventHubName));
        }

        public EventHubProducerClient GetEventHubProducerClient()
        {
            return new EventHubProducerClient(_eventHubNamespace, _eventHubName, GetTokenCredential());
        }
    }
}
