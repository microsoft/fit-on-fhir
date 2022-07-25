// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.FitOnFhir.Common.Interfaces;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.Health.FitOnFhir.Common.Services
{
    public class OpenIdConfigurationProvider : IOpenIdConfigurationProvider
    {
        private HttpClient _httpClient;

        public OpenIdConfigurationProvider(HttpClient httpClient)
        {
            _httpClient = EnsureArg.IsNotNull(httpClient, nameof(httpClient));
        }

        public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(string authority, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(authority, nameof(authority));
            string address = authority.TrimEnd('/') + "/.well-known/openid-configuration";
            return await OpenIdConnectConfigurationRetriever.GetAsync(address, _httpClient, cancellationToken);
        }
    }
}
