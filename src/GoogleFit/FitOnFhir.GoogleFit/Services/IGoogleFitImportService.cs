// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.FitOnFhir.GoogleFit.Client.Models;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Responses;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Services
{
    public interface IGoogleFitImportService
    {
        /// <summary>
        /// Performs Dataset requests in parallel against the list of <see cref="DataSource"/> provided by <param ref="dataSources"></param>
        /// </summary>
        /// <param name="user">The GoogleFit User the data is being imported for.<param ref="dataSources"></param>.</param>
        /// <param name="dataSources">The list of <see cref="DataSource"/> for this user, that have been granted access.</param>
        /// <param name="tokensResponse">The <see cref="AuthTokensResponse"/> which contains the access token.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel this operation, if necessary.</param>
        /// <returns>An empty Task.</returns>
        public Task ProcessDatasetRequests(
            GoogleFitUser user,
            IEnumerable<DataSource> dataSources,
            AuthTokensResponse tokensResponse,
            CancellationToken cancellationToken);
    }
}
