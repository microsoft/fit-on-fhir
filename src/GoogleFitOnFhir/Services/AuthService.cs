using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Fitness.v1;
using GoogleFitOnFhir.Clients.GoogleFit;
using GoogleFitOnFhir.Clients.GoogleFit.Requests;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;

namespace GoogleFitOnFhir.Services
{
    public class AuthService : IAuthService
    {
        private readonly ClientContext clientContext;

        public AuthService(ClientContext clientContext)
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

        private IAuthorizationCodeFlow GetAuthFlow()
        {
            // TODO: Customize datastore to use KeyVault
            return new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
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