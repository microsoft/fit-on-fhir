// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Tests.Mocks
{
    public class MockEventHubProducerClient : EventHubProducerClient
    {
        private List<EventData> _eventData;
        private int _sendAsyncCalls;
        private int _createBatchAsyncCalls;

        public MockEventHubProducerClient()
            : base()
        {
            _eventData = new List<EventData>();
        }

        public IList<EventData> BatchEventData => _eventData;

        public int SendAsyncCalls => _sendAsyncCalls;

        public int CreateBatchAsyncCalls => _createBatchAsyncCalls;

        public override ValueTask<EventDataBatch> CreateBatchAsync(CancellationToken cancellationToken)
        {
            _createBatchAsyncCalls++;
            return new ValueTask<EventDataBatch>(EventHubsModelFactory.EventDataBatch(10000, BatchEventData));
        }

        public override Task SendAsync(EventDataBatch eventBatch, CancellationToken cancellationToken)
        {
            _sendAsyncCalls++;
            return Task.CompletedTask;
        }
    }
}
