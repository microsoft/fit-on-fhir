// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.FitOnFhir.GoogleFit.Client.Models;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Requests;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Responses;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Client
{
    public class GoogleFitClient : IGoogleFitClient
    {
        /// <inheritdoc/>
        public Task<DataSourcesListResponse> DataSourcesListRequest(string accessToken, CancellationToken cancellationToken)
        {
            return new DatasourcesListRequest(accessToken).ExecuteAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<MedTechDataset> DatasetRequest(string accessToken, DataSource dataSource, string dataSetId, int limit, CancellationToken cancellationToken, string pageToken = null)
        {
            return new DatasetRequest(accessToken, dataSource, dataSetId, limit, pageToken).ExecuteAsync(cancellationToken);
        }
    }
}
