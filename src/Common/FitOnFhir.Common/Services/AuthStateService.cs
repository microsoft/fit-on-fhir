// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IdentityModel.Tokens.Jwt;
using System.Web;
using EnsureThat;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Health.FitOnFhir.Common.Config;
using Microsoft.Health.FitOnFhir.Common.ExtensionMethods;
using Microsoft.Health.FitOnFhir.Common.Interfaces;
using Microsoft.Health.FitOnFhir.Common.Models;

namespace Microsoft.Health.FitOnFhir.Common.Services
{
    public class AuthStateService : IAuthStateService
    {
        private readonly AuthenticationConfiguration _authenticationConfiguration;
        private readonly IJwtSecurityTokenHandlerProvider _jwtSecurityTokenHandlerProvider;
        private readonly ILogger _logger;

        public AuthStateService(
            AuthenticationConfiguration authenticationConfiguration,
            IJwtSecurityTokenHandlerProvider jwtSecurityTokenHandlerProvider,
            ILogger<AuthStateService> logger)
        {
            _authenticationConfiguration = EnsureArg.IsNotNull(authenticationConfiguration, nameof(authenticationConfiguration));
            _jwtSecurityTokenHandlerProvider = EnsureArg.IsNotNull(jwtSecurityTokenHandlerProvider, nameof(jwtSecurityTokenHandlerProvider));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        public string ExternalIdentifier { get; set; }

        public string ExternalSystem { get; set; }

        /// <inheritdoc/>
        public AuthState CreateAuthState(HttpRequest httpRequest)
        {
            var externalId = HttpUtility.UrlDecode(httpRequest.Query[Constants.ExternalIdQueryParameter]);

            var externalSystem = HttpUtility.UrlDecode(httpRequest.Query[Constants.ExternalSystemQueryParameter]);

            // is this for anonymous logins?
            if (_authenticationConfiguration.IsAnonymousLoginEnabled)
            {
                ExternalIdentifier = EnsureArg.IsNotNullOrWhiteSpace(externalId, nameof(externalId));
                ExternalSystem = EnsureArg.IsNotNullOrWhiteSpace(externalSystem, nameof(externalSystem));
            }
            else
            {
                // do not allow the ExternalId or ExternalSystem query params when authentication is enabled
                if (!string.IsNullOrEmpty(externalId) || !string.IsNullOrEmpty(externalSystem))
                {
                    string errorMessage = $"{Constants.ExternalIdQueryParameter} and {Constants.ExternalSystemQueryParameter} are forbidden query parameters with non-anonymous authorization.";
                    _logger.LogError(errorMessage);
                    throw new ArgumentException(errorMessage);
                }

                // extract the token from the header.
                if (!httpRequest.TryGetTokenStringFromAuthorizationHeader(JwtBearerDefaults.AuthenticationScheme, out string token))
                {
                    _logger.LogError("The request Authorization header is invalid.");
                    return default;
                }

                // Read the token
                _jwtSecurityTokenHandlerProvider.SetMapInboundClaims(false);
                var jwtSecurityToken = _jwtSecurityTokenHandlerProvider.ReadJwtToken(token);

                if (jwtSecurityToken == default)
                {
                    return default;
                }

                ExternalIdentifier = jwtSecurityToken.Subject;
                ExternalSystem = jwtSecurityToken.Issuer;
            }

            return new AuthState(ExternalIdentifier, ExternalSystem);
        }
    }
}
