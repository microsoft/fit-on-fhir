// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.FitOnFhir.GoogleFit.Client.Models;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Responses;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Client
{
    public interface IGoogleFitClient
    {
        /// <summary>
        /// Wrapper around a GET request made to the "datasources" endpoint.  Lists all data sources that are visible to the developer,
        /// using the OAuth scopes provided. The list is not exhaustive; the user may have private data sources that are only visible to
        /// other developers, or calls using other scopes.
        /// </summary>
        /// <param name="accessToken">The authorized access token for the request.</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the request, if necessary.</param>
        /// <returns>The <see cref="DataSourcesListResponse"/> which contains a list of all the datasource IDs, as strings.</returns>
        Task<DataSourcesListResponse> DataSourcesListRequest(string accessToken, CancellationToken cancellationToken);

        /// <summary>
        /// Wrapper around a GET request made to a specific data source, for a specific data set.
        /// </summary>
        /// <param name="accessToken">The authorized access token for the request.</param>
        /// <param name="dataSource">The <see cref="DataSource"/> which contains stream ID of the source that created the dataset.</param>
        /// <param name="dataSetId">Dataset identifier that is a composite of the minimum data point start time and maximum data point
        /// end time represented as nanoseconds from the epoch. The ID is formatted like: "startTime-endTime"
        /// where startTime and endTime are 64 bit integers.</param>
        /// <param name="limit">The limit to the number of points returned in the request.</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the request, if necessary.</param>
        /// <param name="pageToken">The continuation token, which is used to page through large result sets. To get the next page of
        /// results, set this parameter to the value of nextPageToken from the previous response.</param>
        /// <returns>The dataset result formatted as a <see cref="MedTechDataset"/>.</returns>
        Task<MedTechDataset> DatasetRequest(string accessToken, DataSource dataSource, string dataSetId, int limit, CancellationToken cancellationToken, string pageToken = null);
    }
}
