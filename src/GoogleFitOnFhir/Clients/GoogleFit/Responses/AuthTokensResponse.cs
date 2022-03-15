// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Google.Apis.Auth.OAuth2.Responses;
using System.IdentityModel.Tokens.Jwt;

namespace GoogleFitOnFhir.Clients.GoogleFit.Responses
{
    public class AuthTokensResponse
    {
        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

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