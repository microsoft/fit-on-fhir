using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Fitness.v1;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;

namespace GoogleFitOnFhir.Clients.GoogleFit.Requests
{
    public class AuthTokensRequest
    {
        private readonly ClientContext clientContext;

        private readonly string authCode;

        private readonly IAuthorizationCodeFlow authFlow;

        public AuthTokensRequest(ClientContext clientContext, string authCode, IAuthorizationCodeFlow authFlow)
        {
            this.clientContext = clientContext;
            this.authCode = authCode;
            this.authFlow = authFlow;
        }

        public async Task<AuthTokensResponse> ExecuteAsync()
        {
            TokenResponse tokenResponse = await this.authFlow
                .ExchangeCodeForTokenAsync(
                    "me",
                    this.authCode,
                    this.clientContext.CallbackUri,
                    CancellationToken.None);

            if (tokenResponse != null && tokenResponse.RefreshToken != null)
            {
                var response = new AuthTokensResponse();
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