namespace GoogleFitOnFhir.Persistence
{
    public class ConnectionStringContext
    {
        public ConnectionStringContext(string connectionString)
        {
            this.ConnectionString = connectionString;
        }

        public string ConnectionString { get; set; }
    }
}