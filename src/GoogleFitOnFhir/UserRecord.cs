namespace GoogleFitOnFhir
{
    public class UserRecord : TableEntity
    {
        public UserRecord(string skey, string srow)
        {
            this.PartitionKey = skey;
            this.RowKey = srow;
        }

        public UserRecord() { }

        public int UserId { get; set; }
        public DateTime LastSync { get; set; }
    }
}
