// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.FitOnFhir.Common.Config
{
    public class AuthenticationConfiguration
    {
        private string[] _providerMetadataEndpoints = new string[] { };
        private string[] _approvedRedirectUrls = new string[] { };

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
        public virtual IEnumerable<string> TokenAuthorities => _providerMetadataEndpoints;

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

        /// <summary>
        /// The list of user allowed identity providers.
        /// </summary>
        public virtual IEnumerable<string> ApprovedRedirectUrls => _approvedRedirectUrls;

        /// <summary>
        /// A comma delimited list of user approved redirect URLs.
        /// </summary>
        public string RedirectUrls
        {
            get
            {
                if (_approvedRedirectUrls != null && _approvedRedirectUrls.Any())
                {
                    return string.Join(", ", _approvedRedirectUrls);
                }

                return null;
            }

            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _approvedRedirectUrls = value.Replace(" ", string.Empty).Split(',');
                    return;
                }
            }
        }
    }
}
