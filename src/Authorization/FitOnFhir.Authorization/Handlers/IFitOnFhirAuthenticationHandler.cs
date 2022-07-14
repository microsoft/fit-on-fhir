// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using FitOnFhir.Common.Config;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace FitOnFhir.Authorization.Handlers
{
    public interface IFitOnFhirAuthenticationHandler
    {
        /// <summary>
        /// Validates an access token using the provided issuer's metadata.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/> containing the access token.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to cancel the process.</param>
        /// <returns>true if the token is validated, false if not.</returns>
        public Task<bool> AuthenticateToken(HttpRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a mapping between the metadata endpoints provided in <see cref="AuthenticationConfiguration"/>.AuthorizedIdentityProviders
        /// and the name of the issuer, as declared in the <see cref="OpenIdConnectConfiguration"/> Issuer property for that endpoint's config.
        /// This mapping can be used to determine which endpoint to authenticate tokens against, when a user wishes to authorize access to a fitness data provider.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to cancel the process.</param>
        public Task CreateIssuerMapping(CancellationToken cancellationToken);
    }
}
