// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Google.Apis.Auth.OAuth2.Flows;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;

namespace GoogleFitOnFhir.Services
{
    public interface IGoogleFitAuthUriRequest : IAuthRequest<AuthUriResponse>
    {
        /// <summary>
        /// Set the Google Fit client context and authorization code flow for this particular request.
        /// </summary>
        /// <param name="authFlow">The specific <see cref="IAuthorizationCodeFlow"/>.</param>
        public void SetAuthFlow(IAuthorizationCodeFlow authFlow);
    }
}
