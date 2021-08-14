using Google.Apis.Auth.OAuth2.Responses;

namespace GoogleFitOnFhir.Clients.GoogleFit.Responses
{
    public class AuthTokensResponse
    {
        public AuthTokensResponse()
        {
        }

        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }
    }
}