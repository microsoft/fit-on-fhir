// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using GoogleFitOnFhir.Clients.GoogleFit.Models;
using GoogleFitOnFhir.Clients.GoogleFit.Requests;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;
using GoogleFitOnFhir.Common;

namespace GoogleFitOnFhir.Clients.GoogleFit
{
    public class GoogleFitClient : IGoogleFitClient
    {
        /// <inheritdoc/>
        public Task<MyEmailResponse> MyEmailRequest(string accessToken, CancellationToken cancellationToken)
        {
            return new MyEmailRequest(accessToken).ExecuteAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<DatasourcesListResponse> DatasourcesListRequest(string accessToken, CancellationToken cancellationToken)
        {
            return new DatasourcesListRequest(accessToken).ExecuteAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<IomtDataset> DatasetRequest(string accessToken, string dataSourceId, string dataSetId, CancellationToken cancellationToken, string pageToken = null)
        {
            return new DatasetRequest(accessToken, dataSourceId, dataSetId, GoogleFitDataImporterContext.GoogleFitDatasetRequestLimit, pageToken).ExecuteAsync(cancellationToken);
        }
    }
}