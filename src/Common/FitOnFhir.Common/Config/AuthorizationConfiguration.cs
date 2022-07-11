// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace FitOnFhir.Common.Config
{
    public class AuthorizationConfiguration
    {
        /// <summary>
        /// Indicates whether anonymous logins are allowed
        /// </summary>
        public bool IsAnonymousLoginEnabled { get; set; }

        /// <summary>
        /// A list of the identity providers for auth token verification.
        /// This will be empty if IsAnonymousLoginEnabled is false.
        /// </summary>
        public string IdentityProviders { get; set; }
    }
}
