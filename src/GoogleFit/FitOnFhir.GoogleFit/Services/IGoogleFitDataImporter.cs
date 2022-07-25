// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.FitOnFhir.GoogleFit.Services
{
    public interface IGoogleFitDataImporter
    {
        /// <summary>
        /// Imports data from all DataSources authorized for this user.
        /// </summary>
        /// <param name="userId">The Users partition ID for this user.</param>
        /// /// <param name="googleFitId">The GoogleFit user ID for this user.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        Task Import(string userId, string googleFitId, CancellationToken cancellationToken);
    }
}
