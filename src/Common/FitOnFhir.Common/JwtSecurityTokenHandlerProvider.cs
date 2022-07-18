// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IdentityModel.Tokens.Jwt;
using EnsureThat;
using Microsoft.IdentityModel.Tokens;

namespace FitOnFhir.Common
{
    public class JwtSecurityTokenHandlerProvider : IJwtSecurityTokenHandlerProvider
    {
        private JwtSecurityTokenHandler _jwtSecurityTokenHandler;

        /// <inheritdoc/>
        public void CreateJwtSecurityTokenHandler()
        {
            _jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
        }

        /// <inheritdoc/>
        public JwtSecurityToken ReadJwtToken(string token)
        {
            var jwtSecurityTokenHandler = EnsureArg.IsNotNull(_jwtSecurityTokenHandler, nameof(_jwtSecurityTokenHandler));
            return jwtSecurityTokenHandler.ReadJwtToken(token);
        }

        /// <inheritdoc/>
        public Task<TokenValidationResult> ValidateTokenAsync(string token, TokenValidationParameters validationParameters)
        {
            var jwtSecurityTokenHandler = EnsureArg.IsNotNull(_jwtSecurityTokenHandler, nameof(_jwtSecurityTokenHandler));
            return jwtSecurityTokenHandler.ValidateTokenAsync(token, validationParameters);
        }

        /// <inheritdoc/>
        public void SetMapInboundClaims(bool mappingEnabled)
        {
            var jwtSecurityTokenHandler = EnsureArg.IsNotNull(_jwtSecurityTokenHandler, nameof(_jwtSecurityTokenHandler));
            jwtSecurityTokenHandler.MapInboundClaims = mappingEnabled;
        }
    }
}
