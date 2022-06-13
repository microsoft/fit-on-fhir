// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.GoogleFit.Client.Models;
using FitOnFhir.GoogleFit.Client.Requests;
using FitOnFhir.GoogleFit.Client.Responses;

namespace FitOnFhir.GoogleFit.Client
{
    public class GoogleFitClient : IGoogleFitClient
    {
        /// <inheritdoc/>
        public Task<MyEmailResponse> MyEmailRequest(string accessToken, CancellationToken cancellationToken)
        {
            return new MyEmailRequest(accessToken).ExecuteAsync(cancellationToken);
        }

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