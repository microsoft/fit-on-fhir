using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GoogleFitOnFhir.PublishData
{
    public static class PublishData
    {
        [FunctionName("PublishData")]
        public static async Task Run([QueueTrigger("myqueue-items", Connection = "")] string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");

            // TODO: iomtConnectingString from env var or key vault?
            string iomtConnectionString = "";
            // TODO: Retrieve refresh token for user
            // TODO: Get access token and new refresh token from Google Identity
            string accessToken = "";
            // TODO: Store new refresh token

            GoogleFitData googleFitData = new GoogleFitData(accessToken);
            var datasourceList = googleFitData.GetDataSourceList();

            // Filter by dataType, first example using com.google.blood_glucose
            // Datasource.Type "raw" is an original datasource
            // Datasource.Type "derived" is a combination/merge of raw datasources
            var glucoseDataSourcesDataStreamIds = datasourceList.DataSource
                .Where(d => d.DataType.Name == "com.google.blood_glucose" && d.Type == "raw")
                .Select(d => d.DataStreamId);

            var producerClient = new EventHubProducerClient(iomtConnectionString);
            // Create a batch of events for IoMT eventhub
            using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();

            // Get dataset for each dataSource
            foreach (var dataStreamId in glucoseDataSourcesDataStreamIds)
            {
                // TODO: Generate datasetId based on event type
                //       last 30 days to beginning of hour for first migration
                //       previous hour for interval migration
                var dataset = googleFitData.GetDataset(dataStreamId, "1574159699023000000-1574159699023000000");

                // Add user id to payload
                // TODO: Use userId from queue message
                dataset.UserId = "testUserId";

                // Push dataset to IoMT connector
                if (!eventBatch.TryAdd(new EventData(JsonConvert.SerializeObject(dataset))))
                {
                    throw new Exception($"Event is too large for the batch and cannot be sent.");
                }
            }

            try
            {
                // Use the producer client to send the batch of events to the event hub
                await producerClient.SendAsync(eventBatch);
                log.LogInformation("A batch of events has been published.");
            }
            finally
            {
                await producerClient.DisposeAsync();
            }
        }
    }
}
