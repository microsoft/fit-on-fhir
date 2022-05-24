// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using FitOnFhir.GoogleFit.Client.Responses;

namespace FitOnFhir.GoogleFit.Services
{
    /// <summary>
    /// Provides methods for authorizing a user.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Sends an authorization request to the platform appropriate URI.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to cancel the process.</param>
        /// <returns>The <see cref="AuthUriResponse"/> for the operation.</returns>
        Task<AuthUriResponse> AuthUriRequest(CancellationToken cancellationToken);

        /// <summary>
        /// Sends a request to the platform appropriate URI for the authorization tokens.
        /// </summary>
        /// <param name="authCode">The authorization code used to make the tokens request.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to cancel the process.</param>
        /// <returns>The <see cref="AuthTokensResponse"/> for the operation.</returns>
        Task<AuthTokensResponse> AuthTokensRequest(string authCode, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a tokens refresh request to the platform appropriate URI.
        /// </summary>
        /// <param name="refreshToken">The refresh token value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to cancel the process.</param>
        /// <returns>The <see cref="AuthTokensResponse"/> for the operation.</returns>
        Task<AuthTokensResponse> RefreshTokensRequest(string refreshToken, CancellationToken cancellationToken);
    }
}