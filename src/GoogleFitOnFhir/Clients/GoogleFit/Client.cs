using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Fitness.v1;
using GoogleFitOnFhir.Clients.GoogleFit.Models;
using GoogleFitOnFhir.Clients.GoogleFit.Requests;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;

namespace GoogleFitOnFhir.Clients.GoogleFit
{
    public class Client
    {
        private ClientContext clientContext;

        public Client(ClientContext clientContext)
        {
            this.clientContext = clientContext;
        }

        public async Task<AuthUriResponse> AuthUriRequest(string authCode)
        {
            var request = new AuthUriRequest(this.clientContext, this.GetAuthFlow());
            return await request.ExecuteAsync();
        }

        public async Task<AuthTokensResponse> AuthTokensRequest(string authCode)
        {
            var request = new AuthTokensRequest(this.clientContext, authCode, this.GetAuthFlow());
            return await request.ExecuteAsync();
        }

        public async Task<MyEmailResponse> MyEmailRequest(string accessToken)
        {
            var request = new MyEmailRequest(accessToken);
            return await request.ExecuteAsync();
        }

        public async Task<DatasourcesListResponse> DatasourcesListRequest(string accessToken)
        {
            var request = new DatasourcesListRequest(accessToken);
            return await request.ExecuteAsync();
        }

        public async Task<IomtDataset> DatasetRequest(string accessToken, string dataStreamId, string dataSetId)
        {
            var request = new DatasetRequest(accessToken, dataStreamId, dataSetId);
            return await request.ExecuteAsync();
        }

        private IAuthorizationCodeFlow GetAuthFlow()
        {
            // TODO: Customize datastore to use KeyVault
            return new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                // TODO: Securely store and make ClientId/ClientSecret available
                ClientSecrets = new ClientSecrets
                {
                    ClientId = this.clientContext.ClientId,
                    ClientSecret = this.clientContext.ClientSecret,
                },

                // TODO: Only need write scopes for e2e tests - make this dynamic
                Scopes = new[]
                {
                    "https://www.googleapis.com/auth/userinfo.email",
                    "https://www.googleapis.com/auth/userinfo.profile",
                    FitnessService.Scope.FitnessBloodGlucoseRead,
                    FitnessService.Scope.FitnessBloodGlucoseWrite,
                    FitnessService.Scope.FitnessHeartRateRead,
                    FitnessService.Scope.FitnessHeartRateWrite,
                },
            });
        }
    }
}