using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2.Flows;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;

namespace GoogleFitOnFhir.Services
{
    public interface IAuthService
    {
        Task<AuthUriResponse> AuthUriRequest();

        Task<AuthTokensResponse> AuthTokensRequest(string authCode);

        Task<AuthTokensResponse> RefreshTokensRequest(string refreshToken);
    }
}