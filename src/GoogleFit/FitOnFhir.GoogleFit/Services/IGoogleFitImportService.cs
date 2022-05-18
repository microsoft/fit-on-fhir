// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitOnFhir.Common.Models;
using FitOnFhir.GoogleFit.Clients.GoogleFit.Models;
using FitOnFhir.GoogleFit.Clients.GoogleFit.Responses;

namespace FitOnFhir.GoogleFit.Services
{
    public interface IGoogleFitImportService
    {
        /// <summary>
        /// Performs Dataset requests in parallel against the list of <see cref="DataSource"/> provided by <param ref="dataSources"></param>
        /// </summary>
        /// <param name="user">The user ID associated with the <param ref="dataSources"></param>.</param>
        /// <param name="dataSources">The list of <see cref="DataSource"/> for this user, that have been granted access.</param>
        /// <param name="datasetId">An ID that corresponds to a timestamp range.  All data within this timestamp range will be returned for each <see cref="DataSource"/> specified.</param>
        /// <param name="tokensResponse">The <see cref="AuthTokensResponse"/> which contains the access token.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel this operation, if necessary.</param>
        /// <returns>An empty Task.</returns>
        public Task ProcessDatasetRequests(
            User user,
            IEnumerable<DataSource> dataSources,
            string datasetId,
            AuthTokensResponse tokensResponse,
            CancellationToken cancellationToken);
    }
}
