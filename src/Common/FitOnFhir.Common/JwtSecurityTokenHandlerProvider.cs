// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace FitOnFhir.Common
{
    public class JwtSecurityTokenHandlerProvider : IJwtSecurityTokenHandlerProvider
    {
        private JwtSecurityTokenHandler _jwtSecurityTokenHandler;

        public JwtSecurityTokenHandlerProvider()
        {
            _jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
        }

        /// <inheritdoc/>
        public JwtSecurityToken ReadJwtToken(string token)
        {
            return _jwtSecurityTokenHandler.ReadJwtToken(token);
        }

        /// <inheritdoc/>
        public Task<TokenValidationResult> ValidateTokenAsync(string token, TokenValidationParameters validationParameters)
        {
            return _jwtSecurityTokenHandler.ValidateTokenAsync(token, validationParameters);
        }

        /// <inheritdoc/>
        public void SetMapInboundClaims(bool mappingEnabled)
        {
            _jwtSecurityTokenHandler.MapInboundClaims = mappingEnabled;
        }
    }
}
