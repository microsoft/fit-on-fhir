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
    }
}
