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

        public Task<AuthUriResponse> AuthUriRequest()
        {
            return new AuthUriRequest(this.clientContext, this.GetAuthFlow())
                .ExecuteAsync();
        }

        public Task<AuthTokensResponse> AuthTokensRequest(string authCode)
        {
            return new AuthTokensRequest(this.clientContext, authCode, this.GetAuthFlow())
                .ExecuteAsync();
        }

        public Task<AuthTokensResponse> RefreshTokensRequest(string refreshToken)
        {
            return new RefreshTokensRequest(this.clientContext, refreshToken, this.GetAuthFlow())
                .ExecuteAsync();
        }

        public Task<MyEmailResponse> MyEmailRequest(string accessToken)
        {
            return new MyEmailRequest(accessToken).ExecuteAsync();
        }

        public Task<DatasourcesListResponse> DatasourcesListRequest(string accessToken)
        {
            return new DatasourcesListRequest(accessToken)
                .ExecuteAsync();
        }

        public Task<IomtDataset> DatasetRequest(string accessToken, string dataStreamId, string dataSetId)
        {
            return new DatasetRequest(accessToken, dataStreamId, dataSetId)
                .ExecuteAsync();
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