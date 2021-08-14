using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Web;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;

namespace GoogleFitOnFhir.Clients.GoogleFit.Requests
{
    public class AuthUriRequest
    {
        private ClientContext clientContext;

        private IAuthorizationCodeFlow authFlow;

        public AuthUriRequest(ClientContext clientContext, IAuthorizationCodeFlow authFlow)
        {
            this.clientContext = clientContext;
            this.authFlow = authFlow;
        }

        public async Task<AuthUriResponse> ExecuteAsync()
        {
            var request = new AuthorizationCodeWebApp(
                this.authFlow,
                this.clientContext.CallbackUri,
                string.Empty);

            var result = await request.AuthorizeAsync("user", CancellationToken.None);
            if (result.Credential == null)
            {
                var response = new AuthUriResponse();
                response.Uri = result.RedirectUri;
                return response;
            }
            else
            {
                // Not sure when this would happen
                return null;
            }
        }
    }
}