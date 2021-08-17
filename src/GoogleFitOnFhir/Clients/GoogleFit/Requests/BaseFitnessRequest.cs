using System.Diagnostics.CodeAnalysis;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Fitness.v1;
using Google.Apis.Services;

namespace GoogleFitOnFhir.Clients.GoogleFit.Requests
{
    public class BaseFitnessRequest
    {
        public BaseFitnessRequest(string accessToken)
        {
            this.AccessToken = accessToken;
            this.FitnessService = new FitnessService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = GoogleCredential.FromAccessToken(accessToken),
            });
        }

        protected FitnessService FitnessService { get; }

        protected string AccessToken { get; }
    }
}