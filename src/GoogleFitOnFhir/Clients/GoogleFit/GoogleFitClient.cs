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

        public Task<DatasourcesListResponse> DatasourcesListRequest(string accessToken, CancellationToken cancellationToken)
        {
            return new DatasourcesListRequest(accessToken).ExecuteAsync(cancellationToken);
        }

        public Task<IomtDataset> DatasetRequest(string accessToken, string dataStreamId, string dataSetId, CancellationToken cancellationToken)
        {
            return new DatasetRequest(accessToken, dataStreamId, dataSetId).ExecuteAsync(cancellationToken);
        }
    }
}