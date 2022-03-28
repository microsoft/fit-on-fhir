// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Fitness.v1;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;

namespace GoogleFitOnFhir.Clients.GoogleFit.Requests
{
    public class DatasourcesListRequest : BaseFitnessRequest
    {
        public DatasourcesListRequest(string accessToken)
        : base(accessToken)
        {
        }

        public async Task<DatasourcesListResponse> ExecuteAsync(CancellationToken cancellationToken)
        {
            var listRequest = new UsersResource.DataSourcesResource.ListRequest(FitnessService, "me");
            var datasourceList = await listRequest.ExecuteAsync(cancellationToken);

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