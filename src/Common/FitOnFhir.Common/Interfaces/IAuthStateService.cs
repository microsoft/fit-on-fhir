// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Health.FitOnFhir.Common.Config;
using Microsoft.Health.FitOnFhir.Common.Models;

namespace Microsoft.Health.FitOnFhir.Common.Interfaces
{
    public interface IAuthStateService
    {
        /// <summary>
        /// Creates an <see cref="AuthState"/> object that varies, depending on the <see cref="AuthenticationConfiguration.IsAnonymousLoginEnabled"/> setting.
        /// In cases where IsAnonymousLoginEnabled is true, the AuthState properties will reflect the query params <see cref="Constants.ExternalIdQueryParameter"/>
        /// and <see cref="Constants.ExternalSystemQueryParameter"/>.  In cases where IsAnonymousLoginEnabled is false, the AuthState properties will reflect
        /// the subject and issuer claims in the bearer token.
        /// </summary>
        /// <param name="httpRequest">The <see cref="HttpRequest"/> containing the query parameters or bearer token.</param>
        /// <returns>A populated <see cref="AuthState"/> to be used in authorizing with the appropriate device platform.
        /// Will throw an <see cref="ArgumentException"/> if <see cref="AuthenticationConfiguration.IsAnonymousLoginEnabled"/> is false and
        /// query params <see cref="Constants.ExternalIdQueryParameter"/> and <see cref="Constants.ExternalSystemQueryParameter"/> are present in the <see cref="HttpRequest"/>.</returns>
        AuthState CreateAuthState(HttpRequest httpRequest);

        /// <summary>
        /// Stores an <see cref="AuthState"/> as a blob under a generated nonce name.
        /// The generated nonce returned is used later on for retrieval of the AuthState data.
        /// </summary>
        /// <param name="state">The <see cref="AuthState"/> to store.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to cancel the process.</param>
        /// <returns>The generated nonce which the <see cref="AuthState"/> is stored under.</returns>
        Task<string> StoreAuthState(AuthState state, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves stored <see cref="AuthState"/> data from a blob, as identified by the provided nonce.
        /// </summary>
        /// <param name="nonce">The nonce which the blob is named after.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to cancel the process.</param>
        /// <returns>The <see cref="AuthState"/> stored in the blob specified by the provided nonce.</returns>
        Task<AuthState> RetrieveAuthState(string nonce, CancellationToken cancellationToken);
    }
}
