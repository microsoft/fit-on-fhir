using System;
using Azure;
using Azure.Data.Tables;

namespace GoogleFitOnFhir
{
    public class UserRecord : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string UserId { get; set; }
        public DateTimeOffset? LastSync { get; set; }

        public UserRecord(string userId)
        {
            this.PartitionKey = "users-partition";
            this.RowKey = userId;
            this.UserId = userId;
        }

        public UserRecord() { }
    }
}
