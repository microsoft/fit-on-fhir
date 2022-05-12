// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using GoogleFitOnFhir.Clients.GoogleFit.Models;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;

namespace GoogleFitOnFhir.Clients.GoogleFit
{
    public interface IGoogleFitClient
    {
        Task<MyEmailResponse> MyEmailRequest(string accessToken, CancellationToken cancellationToken);

        Task<DataSourcesListResponse> DataSourcesListRequest(string accessToken, CancellationToken cancellationToken);

        Task<MedTechDataset> DatasetRequest(string accessToken, DataSource dataSource, string dataSetId, CancellationToken cancellationToken);
    }
}
