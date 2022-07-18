// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace FitOnFhir.Common.Config
{
    public class AuthenticationConfiguration
    {
        private string[] _providerMetadataEndpoints;

        /// <summary>
        /// Indicates whether anonymous logins are allowed.
        /// </summary>
        public bool IsAnonymousLoginEnabled { get; set; }

        /// <summary>
        /// The URL that any auth tokens are granted for.
        /// </summary>
        public string Audience { get; set; }

        /// <summary>
        /// The list of user allowed identity providers.
        /// </summary>
        public virtual IEnumerable<string> IdentityProviderMetadataEndpoints => _providerMetadataEndpoints;

        /// <summary>
        /// A comma delimited list of the identity providers for auth token verification.
        /// This will be empty if IsAnonymousLoginEnabled is false.
        /// </summary>
        public string IdentityProviders
        {
            get
            {
                if (_providerMetadataEndpoints != null && _providerMetadataEndpoints.Any())
                {
                    return string.Join(", ", _providerMetadataEndpoints);
                }

                return null;
            }

            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _providerMetadataEndpoints = value.Replace(" ", string.Empty).Split(',');
                    return;
                }
            }
        }
    }
}
