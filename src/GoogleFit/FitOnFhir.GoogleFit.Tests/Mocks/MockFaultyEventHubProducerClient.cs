// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Tests.Mocks
{
    public class MockFaultyEventHubProducerClient : EventHubProducerClient
    {
        private List<EventData> _eventData;
        private int _sendAsyncCalls;
        private int _createBatchAsyncCalls;

        public MockFaultyEventHubProducerClient()
            : base()
        {
            _eventData = new List<EventData>();
        }

        public IList<EventData> BatchEventData => _eventData;

        public int SendAsyncCalls => _sendAsyncCalls;

        public int CreateBatchAsyncCalls => _createBatchAsyncCalls;

        /// <summary>
        /// Testing override which returns an EventDataBatch with 0 bytes in size, and adds a TryAdd callback method that returns false.
        /// </summary>
        /// <param name="cancellationToken">The token for canceling the operation.</param>
        /// <returns>The <see cref="EventDataBatch"/> with a 0 size.</returns>
        public override ValueTask<EventDataBatch> CreateBatchAsync(CancellationToken cancellationToken)
        {
            _createBatchAsyncCalls++;
            return new ValueTask<EventDataBatch>(EventHubsModelFactory.EventDataBatch(0, BatchEventData, tryAddCallback: TryAddCallback));
        }

        /// <summary>
        /// Used to simulate cases where the EventDataBatch is unable to add the EventData payload.
        /// </summary>
        /// <param name="arg">The <see cref="EventData"/> to add to the batch.</param>
        /// <returns>false, indicating a mock failure.</returns>
        private bool TryAddCallback(EventData arg)
        {
            return false;
        }

        public override Task SendAsync(EventDataBatch eventBatch, CancellationToken cancellationToken)
        {
            _sendAsyncCalls++;
            return Task.CompletedTask;
        }
    }
}
