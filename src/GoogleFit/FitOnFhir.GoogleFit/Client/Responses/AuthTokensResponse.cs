// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IdentityModel.Tokens.Jwt;
using Google.Apis.Auth.OAuth2.Responses;
using Microsoft.Health.FitOnFhir.Common.Models;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Client.Responses
{
    public class AuthTokensResponse : AuthTokenBase
    {
        public JwtSecurityToken IdToken { get; set; }

        public static bool TryParse(TokenResponse tokenResponse, out AuthTokensResponse response)
        {
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

            if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.RefreshToken) && tokenHandler.CanReadToken(tokenResponse.IdToken))
            {
                response = new AuthTokensResponse
                {
                    AccessToken = tokenResponse.AccessToken,
                    RefreshToken = tokenResponse.RefreshToken,
                    IdToken = tokenHandler.ReadJwtToken(tokenResponse.IdToken),
                };

                return true;
            }
            else
            {
                response = null;
                return false;
            }
        }
    }
}