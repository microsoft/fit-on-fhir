﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IdentityModel.Tokens.Jwt;
using EnsureThat;
using FitOnFhir.Common.Config;
using FitOnFhir.Common.ExtensionMethods;
using FitOnFhir.Common.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace FitOnFhir.Common.Services
{
    public class TokenValidationService : ITokenValidationService
    {
        private readonly IOpenIdConfigurationProvider _openIdConfigurationProvider;
        private readonly AuthenticationConfiguration _authenticationConfiguration;
        private readonly IJwtSecurityTokenHandlerProvider _jwtSecurityTokenHandlerProvider;
        private readonly ILogger _logger;
        private readonly Dictionary<string, string> _issuers = new ();
        private readonly List<Task> _configurationTasks = new ();

        public TokenValidationService(
            IOpenIdConfigurationProvider openIdConfigurationProvider,
            AuthenticationConfiguration authenticationConfiguration,
            IJwtSecurityTokenHandlerProvider jwtSecurityTokenHandlerProvider,
            ILogger<TokenValidationService> logger)
        {
            _openIdConfigurationProvider = EnsureArg.IsNotNull(openIdConfigurationProvider, nameof(openIdConfigurationProvider));
            _authenticationConfiguration = EnsureArg.IsNotNull(authenticationConfiguration, nameof(authenticationConfiguration));
            _jwtSecurityTokenHandlerProvider = EnsureArg.IsNotNull(jwtSecurityTokenHandlerProvider, nameof(jwtSecurityTokenHandlerProvider));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));

            CreateIssuerMapping();
        }

        /// <summary>
        /// Indicates whether anonymous logins are allowed.
        /// </summary>
        protected bool IsAnonymousLoginEnabled => _authenticationConfiguration.IsAnonymousLoginEnabled;

        /// <inheritdoc/>
        public async Task<bool> ValidateToken(HttpRequest request, CancellationToken cancellationToken)
        {
            if (IsAnonymousLoginEnabled)
            {
                return true;
            }

            EnsureArg.IsNotNull(request, nameof(request));

            // Ensure the request has a valid bearer token and extract the token from the header.
            if (!request.TryGetTokenStringFromAuthorizationHeader(JwtBearerDefaults.AuthenticationScheme, out string token))
            {
                _logger.LogError("The request Authorization header is invalid.");
                return false;
            }

            // Read the token into the handler.
            _jwtSecurityTokenHandlerProvider.SetMapInboundClaims(false);
            JwtSecurityToken jwtSecurityToken;
            try
            {
                jwtSecurityToken = _jwtSecurityTokenHandlerProvider.ReadJwtToken(token);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "The request JWT is malformed.");
                return false;
            }

            // Wait, if necessary, for all metadata configuration to be returned
            Task t = Task.WhenAll(_configurationTasks);
            await t.WaitAsync(cancellationToken);

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
                var tokenValidationResult = await _jwtSecurityTokenHandlerProvider.ValidateTokenAsync(jwtSecurityToken.RawData, tokenValidationParameters);

                return tokenValidationResult.IsValid;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to validate the request Bearer token.");
                return false;
            }
        }

        /// <summary>
        /// Creates a mapping between the metadata endpoints provided in <see cref="FitOnFhir.Common.Config.AuthenticationConfiguration"/>.AuthorizedIdentityProviders
        /// and the name of the issuer, as declared in the <see cref="OpenIdConnectConfiguration"/> Issuer property for that endpoint's config.
        /// This mapping can be used to determine which endpoint to authenticate tokens against, when a user wishes to authorize access to a fitness data provider.
        /// Configuration data requests are made asynchronously.
        /// </summary>
        private void CreateIssuerMapping()
        {
            foreach (var tokenAuthority in _authenticationConfiguration.TokenAuthorities)
            {
                try
                {
                    _configurationTasks.Add(Task.Run(async () =>
                    {
                        CancellationTokenSource cts = new CancellationTokenSource();
                        cts.CancelAfter(60000);
                        var config =
                            await _openIdConfigurationProvider.GetConfigurationAsync(tokenAuthority, cts.Token);
                        _issuers.Add(config.Issuer, tokenAuthority);
                    }));
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Failed to retrieve the config for authority: {tokenAuthority}");
                }
            }
        }
    }
}