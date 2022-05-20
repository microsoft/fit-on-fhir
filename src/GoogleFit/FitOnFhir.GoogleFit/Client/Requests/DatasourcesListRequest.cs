// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitOnFhir.GoogleFit.Client.Models;
using FitOnFhir.GoogleFit.Client.Responses;
using Google.Apis.Fitness.v1;

namespace FitOnFhir.GoogleFit.Client.Requests
{
    public class DatasourcesListRequest : BaseFitnessRequest
    {
        public DatasourcesListRequest(string accessToken)
        : base(accessToken)
        {
        }

        public async Task<DataSourcesListResponse> ExecuteAsync(CancellationToken cancellationToken)
        {
            var listRequest = new UsersResource.DataSourcesResource.ListRequest(FitnessService, "me");
            var datasourceList = await listRequest.ExecuteAsync(cancellationToken);

            // Extract the DataStreamIds Device.Uids and Application.PackageNames from the response.
            var dataSources = datasourceList.DataSource.Select(d => new DataSource(d.DataStreamId, d.Device?.Uid, d.Application?.PackageName));

            return new DataSourcesListResponse()
            {
                DataSources = dataSources,
            };
        }
    }
}