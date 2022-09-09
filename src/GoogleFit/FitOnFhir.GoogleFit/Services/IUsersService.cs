// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.FitOnFhir.Common;
using Microsoft.Health.FitOnFhir.Common.Config;
using Microsoft.Health.FitOnFhir.Common.Models;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Services
{
    public interface IUsersService
    {
        /// <summary>
        /// Method for processing the callback authorization request made by Google as the final part of authorization for a user.
        /// Using the authorization code provided by Google, tokens (authorization, refresh, and ID) are retrieved for the user.
        /// The ID token's subject and issuer are stored as identifiers for a patient in the FHIR server instance.
        /// </summary>
        /// <param name="authCode">The authorization code provided by Google and used to retrieve tokens.</param>
        /// <param name="nonce">The nonce that was used to authorize with the device platform.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>The URL that the user will be redirected to.</returns>
        Task<Uri> ProcessAuthorizationCallback(string authCode, string nonce, CancellationToken cancellationToken);

        /// <summary>
        /// Inserts a <see cref="QueueMessage"/> for the <see cref="User"/> into the Queue identified by the connection string in
        /// <see cref="AzureConfiguration"/>.StorageAccountConnectionString and name contained in <see cref="Constants"/>.QueueName.
        /// </summary>
        /// <param name="user">The <see cref="User"/> containing the <see cref="PlatformUserInfo"/> data for the <see cref="QueueMessage"/>.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        Task QueueFitnessImport(User user, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves <see cref="PlatformUserInfo"/> for the user associated with the patient in a given <see cref="AuthState"/>.
        /// Uses this info to make a platform specific revoke access request.  Once this revoke access request is complete, updates
        /// the <see cref="PlatformUserInfo"/> with a <see cref="RevokeReason"/> that indicates access was revoked intentionally by a user.
        /// Also updates the <see cref="PlatformUserInfo"/> with the time stamp when this action occurred.
        /// </summary>
        /// <param name="state">The <see cref="AuthState"/> containing the patient ID and system.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        Task RevokeAccess(AuthState state, CancellationToken cancellationToken);
    }
}
