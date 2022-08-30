// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.FitOnFhir.Common.Config;
using Microsoft.Health.FitOnFhir.Common.Interfaces;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using NSubstitute;

namespace Microsoft.Health.FitOnFhir.Common.Tests
{
    public abstract class AuthenticationBaseTests
    {
        private JwtSecurityToken _jwtSecurityToken;
        private List<string> _metadataEndpoints = new List<string>();
        private List<string> _redirectUrls = new List<string>();

        private readonly HttpRequest _httpRequest;
        private readonly AuthenticationConfiguration _authenticationConfiguration;
        private readonly IJwtSecurityTokenHandlerProvider _jwtSecurityTokenHandlerProvider;
        private readonly OpenIdConnectConfiguration _openIdConfiguration;
        private readonly IOpenIdConfigurationProvider _openIdConfigurationProvider;

        public AuthenticationBaseTests()
        {
            // setup substitutes
            _httpRequest = Substitute.For<HttpRequest>();
            _authenticationConfiguration = Substitute.For<AuthenticationConfiguration>();
            _jwtSecurityTokenHandlerProvider = Substitute.For<IJwtSecurityTokenHandlerProvider>();
            _openIdConfiguration = Substitute.For<OpenIdConnectConfiguration>();
            _openIdConfigurationProvider = Substitute.For<IOpenIdConfigurationProvider>();

            SetupJwtSecurityTokenHandlerProvider();
        }

        protected HttpRequest Request => _httpRequest;

        protected AuthenticationConfiguration AuthConfiguration => _authenticationConfiguration;

        protected IJwtSecurityTokenHandlerProvider SecurityTokenHandlerProvider => _jwtSecurityTokenHandlerProvider;

        protected OpenIdConnectConfiguration OpenIdConfiguration => _openIdConfiguration;

        protected IOpenIdConfigurationProvider OpenIdConfigurationProvider => _openIdConfigurationProvider;

        protected string ExpectedExternalIdentifier => "ExternalIdentifier";

        protected string ExpectedExternalSystem => "ExternalSystem";

        protected string ExpectedIssuer => "ExpectedIssuer";

        protected string ExpectedMetadataEndpoint => "ExpectedMetadataEndpoint";

        protected string ExpectedAudience => "ExpectedAudience";

        protected string ExpectedSubject => "ExpectedSubject";

        protected string ExpectedToken => "ExpectedToken";

        protected string TokenAuthScheme => $"{JwtBearerDefaults.AuthenticationScheme} ";

        protected string ExpectedRedirectUrl => "http://localhost/";

        protected string ExpectedState => "ExpectedState";

        protected void SetupConfiguration(bool anonymousLoginEnabled)
        {
            // AuthenticationConfiguration setup
            _authenticationConfiguration.IsAnonymousLoginEnabled = anonymousLoginEnabled;
            _authenticationConfiguration.Audience = ExpectedAudience;
            _metadataEndpoints.Add(ExpectedMetadataEndpoint);
            _authenticationConfiguration.TokenAuthorities.Returns(_metadataEndpoints);
            _authenticationConfiguration.IdentityProviders = ExpectedMetadataEndpoint;
            _redirectUrls.Add(ExpectedRedirectUrl);
            _authenticationConfiguration.ApprovedRedirectUrls.Returns(_redirectUrls);
            _authenticationConfiguration.RedirectUrls = ExpectedRedirectUrl;

            // OpenIdConfiguration retrieval setup
            _openIdConfiguration.Issuer = ExpectedIssuer;
            _openIdConfigurationProvider.GetConfigurationAsync(
                Arg.Is<string>(str => str == ExpectedMetadataEndpoint),
                Arg.Any<CancellationToken>()).Returns(_openIdConfiguration);
        }

        protected void SetupJwtSecurityTokenHandlerProvider()
        {
            // JwtSecurityToken setup
            List<Claim> claims = new List<Claim>();
            claims.Add(new Claim("sub", ExpectedSubject));
            _jwtSecurityToken = new JwtSecurityToken(issuer: ExpectedIssuer, claims: claims);
            _jwtSecurityTokenHandlerProvider.ReadJwtToken(Arg.Is<string>(str => str == ExpectedToken)).Returns(_jwtSecurityToken);

            // TokenValidationResult setup
            TokenValidationResult tvr = new TokenValidationResult() { IsValid = true };
            _jwtSecurityTokenHandlerProvider.ValidateTokenAsync(Arg.Any<string>(), Arg.Any<TokenValidationParameters>()).Returns(tvr);
        }

        protected void SetupHttpRequest(string token, bool includePatient = false, bool includeSystem = false, bool includeState = false, bool includeRedirectUrl = true)
        {
            _httpRequest.Headers.TryGetValue(
                    Arg.Is<string>(str => str == HeaderNames.Authorization),
                    out Arg.Any<StringValues>()).
                Returns(x =>
                {
                    x[1] = new StringValues(TokenAuthScheme + token);
                    return token != string.Empty;
                });

            if (includePatient)
            {
                _httpRequest.Query[Constants.ExternalIdQueryParameter].Returns(new StringValues(ExpectedExternalIdentifier));
            }

            if (includeSystem)
            {
                _httpRequest.Query[Constants.ExternalSystemQueryParameter].Returns(new StringValues(ExpectedExternalSystem));
            }

            if (includeState)
            {
                _httpRequest.Query[Constants.StateQueryParameter].Returns(new StringValues(ExpectedState));
            }

            if (includeRedirectUrl)
            {
                _httpRequest.Query[Constants.RedirectUrlQueryParameter].Returns(new StringValues(ExpectedRedirectUrl));
            }
        }
    }
}
