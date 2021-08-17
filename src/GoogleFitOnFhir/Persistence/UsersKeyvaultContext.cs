namespace GoogleFitOnFhir.Persistence
{
    public class UsersKeyvaultContext
    {
        public UsersKeyvaultContext(string uri)
        {
            this.Uri = uri;
        }

        public string Uri { get; set; }
    }
}