// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.Health.FitOnFhir.Common.Interfaces
{
    public interface IOpenIdConfigurationProvider
    {
        /// <summary>
        /// Gets an OpenIdConnectConfiguration for a given authority.
        /// </summary>
        /// <param name="authority">A token authority string - must be a valid URL.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>an OpenIdConnectConfiguration object.</returns>
        Task<OpenIdConnectConfiguration> GetConfigurationAsync(string authority, CancellationToken cancellationToken);
    }
}
