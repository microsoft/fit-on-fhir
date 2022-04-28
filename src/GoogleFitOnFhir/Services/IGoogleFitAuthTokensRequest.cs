// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using GoogleFitOnFhir.Clients.GoogleFit.Responses;

namespace GoogleFitOnFhir.Services
{
    public interface IGoogleFitAuthTokensRequest : IAuthRequest<AuthTokensResponse>
    {
        /// <summary>
        /// Sets the authorization code for the token request.
        /// </summary>
        /// <param name="authCode">The authorization code.</param>
        public void SetAuthCode(string authCode);
    }
}
