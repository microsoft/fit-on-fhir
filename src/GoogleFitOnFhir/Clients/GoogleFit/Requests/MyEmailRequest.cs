using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.PeopleService.v1;
using Google.Apis.Services;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;

namespace GoogleFitOnFhir.Clients.GoogleFit.Requests
{
    public class MyEmailRequest
    {
        private PeopleServiceService peopleService;

        public MyEmailRequest(string accessToken)
        {
            this.peopleService = new PeopleServiceService(
                new BaseClientService.Initializer()
            {
                HttpClientInitializer =
                    GoogleCredential.FromAccessToken(accessToken),
            });
        }

        public async Task<MyEmailResponse> ExecuteAsync()
        {
            var request = this.peopleService.People.Get("people/me");
            request.PersonFields = "emailAddresses";

            var data = await request.ExecuteAsync();

            var response = new MyEmailResponse();
            response.EmailAddress = data.EmailAddresses[0].Value;
            return response;
        }
    }
}