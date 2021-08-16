namespace GoogleFitOnFhir.Persistence
{
    public class StorageAccountContext : ConnectionStringContext
    {
        public StorageAccountContext(string connectionString)
        : base(connectionString)
        {
        }
    }
}