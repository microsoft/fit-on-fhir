using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Fitness.v1;
using Google.Apis.Fitness.v1.Data;
using Google.Apis.PeopleService.v1;
using Google.Apis.PeopleService.v1.Data;
using Google.Apis.Services;

namespace GoogleFitOnFhir
{
    /*public class GoogleFitData
    {
        private readonly FitnessService fitnessService;
        private readonly PeopleServiceService peopleService;

        public GoogleFitData(string accessToken)
        {
            this.fitnessService = new FitnessService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = GoogleCredential.FromAccessToken(accessToken),
            });
            this.peopleService = new PeopleServiceService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = GoogleCredential.FromAccessToken(accessToken),
            });
        }

        public ListDataSourcesResponse GetDataSourceList()
        {
            var listRequest = new UsersResource.DataSourcesResource.ListRequest(this.fitnessService, "me");
            return listRequest.Execute();
        }

        public IomtDataset GetDataset(string dataStreamId, string datasetId)
        {
            var datasourceRequest = new UsersResource.DataSourcesResource.DatasetsResource.GetRequest(this.fitnessService, "me", dataStreamId, datasetId);
            return new IomtDataset(datasourceRequest.Execute());
        }

        public Person GetMyInfo()
        {
            PeopleResource.GetRequest peopleRequest = this.peopleService.People.Get("people/me");
            peopleRequest.PersonFields = "emailAddresses";
            return peopleRequest.Execute();
        }
    }*/
}
