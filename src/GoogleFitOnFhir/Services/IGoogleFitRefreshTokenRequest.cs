// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Google.Apis.Auth.OAuth2.Flows;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;

namespace GoogleFitOnFhir.Services
{
    public interface IGoogleFitRefreshTokenRequest : IAuthRequest<AuthTokensResponse>
    {
        public void SetRefreshTokenAndAuthFlow(string refreshToken, IAuthorizationCodeFlow authFlow);
    }
}
