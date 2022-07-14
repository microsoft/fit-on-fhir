// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FitOnFhir.Authorization.Services;
using FitOnFhir.Common.Config;
using FitOnFhir.Common.ExtensionMethods;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace FitOnFhir.Authorization.Handlers
{
    public class FitOnFhirAuthenticationHandler : IFitOnFhirAuthenticationHandler
    {
        private IOpenIdConfigurationProvider _openIdConfigurationProvider;
        private AuthenticationConfiguration _authenticationConfiguration;
        private readonly ILogger _logger;
        private readonly Dictionary<string, string> _issuers;

        public FitOnFhirAuthenticationHandler(
            IOpenIdConfigurationProvider openIdConfigurationProvider,
            AuthenticationConfiguration authenticationConfiguration,
            ILogger<FitOnFhirAuthenticationHandler> logger)
        {
            _openIdConfigurationProvider = EnsureArg.IsNotNull(openIdConfigurationProvider, nameof(openIdConfigurationProvider));
            _authenticationConfiguration = EnsureArg.IsNotNull(authenticationConfiguration, nameof(authenticationConfiguration));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
            _issuers = new Dictionary<string, string>();
        }

        /// <inheritdoc/>
        public async Task<bool> AuthenticateToken(HttpRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            // Ensure the request has a valid bearer token and extract the token from the header.
            if (!request.TryGetTokenStringFromAuthorizationHeader(JwtBearerDefaults.AuthenticationScheme, out string token))
            {
                _logger.LogError("The request Authorization header is invalid.");
                return false;
            }

            // Read the token into the handler.
            var handler = new JwtSecurityTokenHandler();
            handler.MapInboundClaims = false;
            JwtSecurityToken jwtSecurityToken;
            try
            {
                jwtSecurityToken = handler.ReadJwtToken(token);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "The request JWT is malformed.");
                return false;
            }

            // Find the correct authority from the list
            if (!_issuers.TryGetValue(jwtSecurityToken.Issuer, out var authority))
            {
                _logger.LogInformation("Issuer {0} not found in the list of authorized identity providers.", jwtSecurityToken.Issuer);
                return false;
            }

            // Fetch the signing keys from the authority.
            var config = await _openIdConfigurationProvider.GetConfigurationAsync(authority, cancellationToken);

            try
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    RequireSignedTokens = true,
                    ValidAudience = _authenticationConfiguration.Audience,
                    ValidateAudience = true,
                    ValidIssuer = config.Issuer,
                    ValidateIssuer = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    IssuerSigningKeys = config.SigningKeys,
                };

                // Validate the token.
                var tokenValidationResult = await handler.ValidateTokenAsync(jwtSecurityToken.RawData, tokenValidationParameters);

                return tokenValidationResult.IsValid;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to validate the request Bearer token.");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task CreateIssuerMapping(CancellationToken cancellationToken)
        {
            try
            {
                foreach (var providerEndpoint in _authenticationConfiguration.IdentityProviderMetadataEndpoints)
                {
                    var config = await _openIdConfigurationProvider.GetConfigurationAsync(providerEndpoint, cancellationToken);
                    _issuers.Add(config.Issuer, providerEndpoint);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to retrieve the config for an issuer");
            }
        }
    }
}