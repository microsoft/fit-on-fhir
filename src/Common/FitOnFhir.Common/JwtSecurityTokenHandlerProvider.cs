// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IdentityModel.Tokens.Jwt;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Health.FitOnFhir.Common
{
    public class JwtSecurityTokenHandlerProvider : IJwtSecurityTokenHandlerProvider
    {
        private JwtSecurityTokenHandler _jwtSecurityTokenHandler;
        private readonly ILogger _logger;

        public JwtSecurityTokenHandlerProvider(ILogger<JwtSecurityTokenHandlerProvider> logger)
        {
            _jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        /// <inheritdoc/>
        public JwtSecurityToken ReadJwtToken(string token)
        {
            try
            {
                return _jwtSecurityTokenHandler.ReadJwtToken(EnsureArg.IsNotNullOrWhiteSpace(token, nameof(token)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "The request JWT is malformed.");
                return default;
            }
        }

        /// <inheritdoc/>
        public Task<TokenValidationResult> ValidateTokenAsync(string token, TokenValidationParameters validationParameters)
        {
            return _jwtSecurityTokenHandler.ValidateTokenAsync(
                EnsureArg.IsNotNullOrWhiteSpace(token, nameof(token)),
                EnsureArg.IsNotNull(validationParameters, nameof(validationParameters)));
        }

        /// <inheritdoc/>
        public void SetMapInboundClaims(bool mappingEnabled)
        {
            _jwtSecurityTokenHandler.MapInboundClaims = mappingEnabled;
        }
    }
}
