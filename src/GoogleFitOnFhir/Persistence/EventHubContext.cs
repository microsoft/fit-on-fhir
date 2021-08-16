namespace GoogleFitOnFhir.Persistence
{
    public class EventHubContext : ConnectionStringContext
    {
        public EventHubContext(string connectionString)
        : base(connectionString)
        {
        }
    }
}