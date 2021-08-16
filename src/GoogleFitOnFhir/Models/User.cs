using System;
using Azure;
using Azure.Data.Tables;

namespace GoogleFitOnFhir.Models
{
    public class User : ITableEntity
    {
        public User(string userId)
        {
            this.PartitionKey = userId;
            this.RowKey = userId;
            this.Id = userId;
        }

        public User()
        {
        }

        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }

        public string Id { get; set; }

        public DateTimeOffset? LastSync { get; set; }
    }
}
