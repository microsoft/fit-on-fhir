using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Fitness.v1;
using Google.Apis.Fitness.v1.Data;
using Google.Apis.Services;

namespace GoogleFitOnFhir
{
    public class GoogleFitData
    {
        protected FitnessService fitnessService;

        public GoogleFitData(string accessToken)
        {
            fitnessService = new FitnessService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = GoogleCredential.FromAccessToken(accessToken)
            });
        }

        public ListDataSourcesResponse GetDataSourceList()
        {
            var listRequest = new UsersResource.DataSourcesResource.ListRequest(fitnessService, "me");
            return listRequest.Execute();
        }

        public IomtDataset GetDataset(string dataStreamId, string datasetId)
        {
            var datasourceRequest = new UsersResource.DataSourcesResource.DatasetsResource.GetRequest(fitnessService, "me", dataStreamId, datasetId);
            return new IomtDataset(datasourceRequest.Execute());
        }
    }
}
