// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.FitOnFhir.Common.Interfaces
{
    public interface ITokensService<TTokenResponse>
    {
        /// <summary>
        /// Method for refreshing an access token with a particular platform.
        /// </summary>
        /// <param name="userId">The user ID for the platform to refresh the token from.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> for canceling the operation.</param>
        /// <returns>A platfrom specific type which contains the info for the relevant tokens.</returns>
        Task<TTokenResponse> RefreshToken(string userId, CancellationToken cancellationToken);
    }
}
