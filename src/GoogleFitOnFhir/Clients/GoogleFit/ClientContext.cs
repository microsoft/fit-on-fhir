namespace GoogleFitOnFhir.Clients.GoogleFit
{
    public class ClientContext
    {
        public ClientContext(string clientId, string clientSecret, string callbackUri)
        {
            this.ClientId = clientId;
            this.ClientSecret = clientSecret;
            this.CallbackUri = callbackUri;
        }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string CallbackUri { get; set; }
    }
}