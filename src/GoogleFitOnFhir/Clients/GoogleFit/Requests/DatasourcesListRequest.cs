using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Fitness.v1;
using Google.Apis.Services;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;

namespace GoogleFitOnFhir.Clients.GoogleFit.Requests
{
    public class DatasourcesListRequest : BaseFitnessRequest
    {
        public DatasourcesListRequest(string accessToken)
        : base(accessToken)
        {
        }

        public async Task<DatasourcesListResponse> ExecuteAsync()
        {
            var listRequest = new UsersResource.DataSourcesResource.ListRequest(this.FitnessService, "me");
            var datasourceList = await listRequest.ExecuteAsync();

            // Filter by dataType, first example using com.google.blood_glucose
            // Datasource.Type "raw" is an original datasource
            // Datasource.Type "derived" is a combination/merge of raw datasources
            var dataStreamIds = datasourceList.DataSource
                .Where(d => d.DataType.Name == "com.google.blood_glucose" && d.Type == "raw")
                .Select(d => d.DataStreamId);

            return new DatasourcesListResponse()
            {
                DatasourceIds = dataStreamIds,
            };
        }
    }
}