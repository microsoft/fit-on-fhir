using System.Collections.Generic;

namespace GoogleFitOnFhir.Clients.GoogleFit.Responses
{
    public class DatasourcesListResponse
    {
        public DatasourcesListResponse()
        {
        }

        public IEnumerable<string> DatasourceIds { get; set; }
    }
}