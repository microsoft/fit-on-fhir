// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.FitOnFhir.Common.Config;
using Microsoft.Health.FitOnFhir.Common.Services;
using Microsoft.Health.FitOnFhir.Common.Tests.Mocks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.FitOnFhir.Common.Tests
{
    public class AuthStateServiceTests
    {
        private readonly HttpRequest _httpRequest;
        private JwtSecurityToken _jwtSecurityToken;
        private List<string> _metadataEndpoints = new List<string>();

        private readonly AuthenticationConfiguration _authenticationConfiguration;
        private readonly IJwtSecurityTokenHandlerProvider _jwtSecurityTokenHandlerProvider;
        private readonly MockLogger<AuthStateService> _logger;
        private readonly AuthStateService _authStateService;

        public AuthStateServiceTests()
        {
            // setup substitutes
            _httpRequest = Substitute.For<HttpRequest>();

            // service dependencies
            _authenticationConfiguration = Substitute.For<AuthenticationConfiguration>();
            _jwtSecurityTokenHandlerProvider = Substitute.For<IJwtSecurityTokenHandlerProvider>();
            _logger = Substitute.For<MockLogger<AuthStateService>>();

            // create new service for testing
            _authStateService = new AuthStateService(_authenticationConfiguration, _jwtSecurityTokenHandlerProvider, _logger);
        }

        protected string ExpectedExternalIdentifier => "ExternalIdentifier";

        protected string ExpectedExternalSystem => "ExternalSystem";

        protected string ExpectedIssuer => "ExpectedIssuer";

        protected string ExpectedMetadataEndpoint => "ExpectedMetadataEndpoint";

        protected string ExpectedAudience => "ExpectedAudience";

        protected string ExpectedSubject => "ExpectedSubject";

        protected string ExpectedToken => "ExpectedToken";

        protected string TokenAuthScheme => $"{JwtBearerDefaults.AuthenticationScheme} ";

        [Fact]
        public void GivenAnonymousLoginEnabledWithValidRequest_WhenCreateAuthStateCalled_ReturnsAuthStateWithQueryParams()
        {
            SetupConfiguration(true);
            SetupJwtSecurityTokenHandlerProvider();
            SetupHttpRequest(ExpectedToken, true);

            var authState = _authStateService.CreateAuthState(_httpRequest);

            Assert.Equal(ExpectedExternalSystem, authState.ExternalSystem);
            Assert.Equal(ExpectedExternalIdentifier, authState.ExternalIdentifier);
        }

        [Fact]
        public void GivenAnonymousLoginDisabledWithValidRequest_WhenCreateAuthStateCalled_ReturnsAuthStateWithSubjectAndIssuer()
        {
            SetupConfiguration(false);
            SetupJwtSecurityTokenHandlerProvider();
            SetupHttpRequest(ExpectedToken, false);

            var authState = _authStateService.CreateAuthState(_httpRequest);

            Assert.Equal(ExpectedIssuer, authState.ExternalSystem);
            Assert.Equal(ExpectedSubject, authState.ExternalIdentifier);
        }

        [Fact]
        public void GivenAnonymousLoginDisabledWithQueryParametersInRequest_WhenCreateAuthStateCalled_ThrowsArgumentException()
        {
            SetupConfiguration(false);
            SetupJwtSecurityTokenHandlerProvider();
            SetupHttpRequest(ExpectedToken, true);

            Assert.Throws<ArgumentException>(() => _authStateService.CreateAuthState(_httpRequest));

            _logger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                Arg.Is<string>(msg =>
                    msg == $"{Constants.ExternalIdQueryParameter} and {Constants.ExternalSystemQueryParameter} are forbidden query parameters with non-anonymous authorization."));
        }

        [Fact]
        public void GivenHttpRequestHasInvalidToken_WhenValidateTokenIsCalled_ReturnsDefaultAndErrorIsLogged()
        {
            SetupConfiguration(false);
            SetupJwtSecurityTokenHandlerProvider();
            SetupHttpRequest(string.Empty);

            var authState = _authStateService.CreateAuthState(_httpRequest);

            Assert.Null(authState);
            _logger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                Arg.Is<string>(msg => msg == "The request Authorization header is invalid."));
        }

        [Fact]
        public void GivenReadJwtTokenReturnsDefault_WhenValidateTokenIsCalled_ReturnsDefaultAndErrorIsLogged()
        {
            SetupConfiguration(false);
            SetupJwtSecurityTokenHandlerProvider();
            SetupHttpRequest(ExpectedToken);

            JwtSecurityToken jwtSecurityToken = default;
            _jwtSecurityTokenHandlerProvider.ReadJwtToken(Arg.Is<string>(str => str == ExpectedToken)).Returns(jwtSecurityToken);

            var authState = _authStateService.CreateAuthState(_httpRequest);

            Assert.Null(authState);
            _logger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                Arg.Is<string>(msg => msg == "The JWT token is empty."));
        }

        private void SetupConfiguration(bool anonymousLoginEnabled)
        {
            // AuthenticationConfiguration setup
            _authenticationConfiguration.IsAnonymousLoginEnabled = anonymousLoginEnabled;
            _authenticationConfiguration.Audience = ExpectedAudience;
            _metadataEndpoints.Add(ExpectedMetadataEndpoint);
            _authenticationConfiguration.TokenAuthorities.Returns(_metadataEndpoints.AsEnumerable());
            _authenticationConfiguration.IdentityProviders = ExpectedMetadataEndpoint;
        }

        private void SetupJwtSecurityTokenHandlerProvider()
        {
            // JwtSecurityToken setup
            List<Claim> claims = new List<Claim>();
            claims.Add(new Claim("sub", ExpectedSubject));
            _jwtSecurityToken = new JwtSecurityToken(issuer: ExpectedIssuer, claims: claims.AsEnumerable());
            _jwtSecurityTokenHandlerProvider.ReadJwtToken(Arg.Is<string>(str => str == ExpectedToken)).Returns(_jwtSecurityToken);

            // TokenValidationResult setup
            TokenValidationResult tvr = new TokenValidationResult() { IsValid = true };
            _jwtSecurityTokenHandlerProvider.ValidateTokenAsync(Arg.Any<string>(), Arg.Any<TokenValidationParameters>()).Returns(tvr);
        }

        private void SetupHttpRequest(string token, bool includePatientAndSystem = false)
        {
            _httpRequest.Headers.TryGetValue(
                    Arg.Is<string>(str => str == HeaderNames.Authorization),
                    out Arg.Any<StringValues>()).
                Returns(x =>
                {
                    x[1] = new StringValues(TokenAuthScheme + token);
                    return token != string.Empty;
                });

            if (includePatientAndSystem)
            {
                _httpRequest.Query[Constants.ExternalIdQueryParameter].Returns(new StringValues(ExpectedExternalIdentifier));
                _httpRequest.Query[Constants.ExternalSystemQueryParameter].Returns(new StringValues(ExpectedExternalSystem));
            }
        }
    }
}
