using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Fitness.v1;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;

namespace GoogleFitOnFhir.Clients.GoogleFit.Requests
{
    public class RefreshTokensRequest
    {
        private ClientContext clientContext;

        private string refreshToken;

        private IAuthorizationCodeFlow authFlow;

        public RefreshTokensRequest(ClientContext clientContext, string refreshToken, IAuthorizationCodeFlow authFlow)
        {
            this.clientContext = clientContext;
            this.refreshToken = refreshToken;
            this.authFlow = authFlow;
        }

        public async Task<AuthTokensResponse> ExecuteAsync()
        {
            TokenResponse tokenResponse = await this.authFlow
                .RefreshTokenAsync(
                    "me",
                    this.refreshToken,
                    CancellationToken.None);

            if (tokenResponse != null && tokenResponse.RefreshToken != null)
            {
                AuthTokensResponse response = new AuthTokensResponse();
                response.AccessToken = tokenResponse.AccessToken;
                response.RefreshToken = tokenResponse.RefreshToken;
                return response;
            }
            else
            {
                return null;
            }
        }
    }
}