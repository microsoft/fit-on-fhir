// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using GoogleFitOnFhir.Clients.GoogleFit.Responses;

namespace GoogleFitOnFhir.Services
{
    public interface IGoogleFitRefreshTokenRequest : IAuthRequest<AuthTokensResponse>
    {
        /// <summary>
        /// Sets the refresh token for the refresh request.
        /// </summary>
        /// <param name="refreshToken">The refresh token.</param>
        public void SetRefreshToken(string refreshToken);
    }
}
