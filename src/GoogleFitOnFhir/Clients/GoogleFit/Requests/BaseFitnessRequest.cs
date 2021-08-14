using System.Diagnostics.CodeAnalysis;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Fitness.v1;
using Google.Apis.Services;

namespace GoogleFitOnFhir.Clients.GoogleFit.Requests
{
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Some fields must be protected")]
    public class BaseFitnessRequest
    {
        protected FitnessService fitnessService;

        protected string accessToken;

        public BaseFitnessRequest(string accessToken)
        {
            this.accessToken = accessToken;
            this.fitnessService = new FitnessService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = GoogleCredential.FromAccessToken(accessToken),
            });
        }
    }
}