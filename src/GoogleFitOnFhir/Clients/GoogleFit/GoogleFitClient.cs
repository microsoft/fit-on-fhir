// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using GoogleFitOnFhir.Clients.GoogleFit.Models;
using GoogleFitOnFhir.Clients.GoogleFit.Requests;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;

namespace GoogleFitOnFhir.Clients.GoogleFit
{
    public class GoogleFitClient : IGoogleFitClient
    {
        public Task<MyEmailResponse> MyEmailRequest(string accessToken, CancellationToken cancellationToken)
        {
            return new MyEmailRequest(accessToken).ExecuteAsync(cancellationToken);
        }

        public Task<DataSourcesListResponse> DataSourcesListRequest(string accessToken, CancellationToken cancellationToken)
        {
            return new DatasourcesListRequest(accessToken).ExecuteAsync(cancellationToken);
        }

        public Task<MedTechDataset> DatasetRequest(string accessToken, DataSource dataSource, string dataSetId, CancellationToken cancellationToken)
        {
            return new DatasetRequest(accessToken, dataSource, dataSetId).ExecuteAsync(cancellationToken);
        }
    }
}