using System.Threading.Tasks;
using Google.Apis.Fitness.v1;
using GoogleFitOnFhir.Clients.GoogleFit.Models;

namespace GoogleFitOnFhir.Clients.GoogleFit.Requests
{
    public class DatasetRequest : BaseFitnessRequest
    {
        private string dataStreamId;

        private string datasetId;

        public DatasetRequest(string accessToken, string dataStreamId, string datasetId)
        : base(accessToken)
        {
            this.dataStreamId = dataStreamId;
            this.datasetId = datasetId;
        }

        public async Task<IomtDataset> ExecuteAsync()
        {
            var datasourceRequest = new UsersResource.DataSourcesResource.DatasetsResource.GetRequest(
                this.FitnessService,
                "me",
                this.dataStreamId,
                this.datasetId);
            var result = await datasourceRequest.ExecuteAsync();
            return new IomtDataset(result);
        }
    }
}