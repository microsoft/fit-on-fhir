// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IdentityModel.Tokens.Jwt;
using FitOnFhir.Authorization.Services;
using FitOnFhir.Common.Config;
using FitOnFhir.Common.Tests.Mocks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace FitOnFhir.Common.Tests
{
    public class TokenValidationServiceTests
    {
        private Dictionary<string, string> _issuers = new Dictionary<string, string>();
        private List<string> _metadataEndpoints = new List<string>();
        private readonly OpenIdConnectConfiguration _openIdConfiguration;
        private readonly HttpRequest _httpRequest;
        private JwtSecurityToken _jwtSecurityToken;

        private readonly IOpenIdConfigurationProvider _openIdConfigurationProvider;
        private readonly AuthenticationConfiguration _authenticationConfiguration;
        private readonly IJwtSecurityTokenHandlerProvider _jwtSecurityTokenHandlerProvider;
        private readonly MockLogger<TokenValidationService> _logger;
        private readonly TokenValidationService _tokenValidationService;

        public TokenValidationServiceTests()
        {
            // setup substitutes
            _openIdConfiguration = Substitute.For<OpenIdConnectConfiguration>();
            _httpRequest = Substitute.For<HttpRequest>();

            // handler dependencies
            _openIdConfigurationProvider = Substitute.For<IOpenIdConfigurationProvider>();
            _authenticationConfiguration = Substitute.For<AuthenticationConfiguration>();
            _jwtSecurityTokenHandlerProvider = Substitute.For<IJwtSecurityTokenHandlerProvider>();
            _logger = Substitute.For<MockLogger<TokenValidationService>>();

            // create new handler for testing
            _tokenValidationService = new TokenValidationService(
                _openIdConfigurationProvider,
                _authenticationConfiguration,
                _jwtSecurityTokenHandlerProvider,
                _logger);
        }

        protected string ExpectedIssuer => "ExpectedIssuer";

        protected string ExpectedMetadataEndpoint => "ExpectedMetadataEndpoint";

        protected string ExpectedAudience => "ExpectedAudience";

        protected string ExpectedToken => "ExpectedToken";

        protected string TokenAuthScheme => $"{JwtBearerDefaults.AuthenticationScheme} ";

        [Fact]
        public async Task GivenIsAnonymousLoginEnabledIsTrue_WhenValidateTokenIsCalled_ReturnsTrue()
        {
            SetupHttpRequest(string.Empty);

            _authenticationConfiguration.IsAnonymousLoginEnabled = true;
            Assert.True(await _tokenValidationService.ValidateToken(_httpRequest, CancellationToken.None));
        }

        [Fact]
        public async Task GivenHttpRequestIsNull_WhenAuthenticateTokenIsCalled_ThrowsArgumentNullException()
        {
            SetupSubstitutes();
            await Assert.ThrowsAsync<ArgumentNullException>(() => _tokenValidationService.ValidateToken(null, CancellationToken.None));
        }

        [Fact]
        public async Task GivenHttpRequestHasInvalidToken_WhenAuthenticateTokenIsCalled_ReturnsFalseAndErrorIsLogged()
        {
            SetupSubstitutes();
            SetupHttpRequest(string.Empty);

            Assert.False(await _tokenValidationService.ValidateToken(_httpRequest, CancellationToken.None));
            _logger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                Arg.Is<string>(msg => msg == "The request Authorization header is invalid."));
        }

        [Fact]
        public async Task GivenTokenIsMalformed_WhenAuthenticateTokenIsCalled_ReturnsFalseAndErrorIsLogged()
        {
            SetupSubstitutes();
            SetupHttpRequest(ExpectedToken);

            string exceptionMessage = "ReadJwtToken exception";
            var exception = new Exception(exceptionMessage);
            _jwtSecurityTokenHandlerProvider.ReadJwtToken(Arg.Is<string>(str => str == ExpectedToken)).Throws(exception);

            Assert.False(await _tokenValidationService.ValidateToken(_httpRequest, CancellationToken.None));
            _logger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                Arg.Any<Exception>(),
                Arg.Is<string>(msg => msg == "The request JWT is malformed."));
        }

        [Fact]
        public async Task GivenNoMappedIssuerMatchesTokenIssuer_WhenAuthenticateTokenIsCalled_ReturnsFalseAndMessageIsLogged()
        {
            SetupSubstitutes();
            SetupHttpRequest(ExpectedToken);

            // Clear out the issuer mapping
            _issuers.Clear();

            Assert.False(await _tokenValidationService.ValidateToken(_httpRequest, CancellationToken.None));
            _logger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Information),
                Arg.Is<string>(msg => msg == $"Issuer {ExpectedIssuer} not found in the list of authorized identity providers."));
        }

        [Fact]
        public async Task GivenValidateTokenAsyncThrowsException_WhenAuthenticateTokenIsCalled_ReturnsFalseAndErrorIsLogged()
        {
            SetupSubstitutes();
            SetupHttpRequest(ExpectedToken);

            string exceptionMessage = "ValidateTokenAsync exception";
            var exception = new Exception(exceptionMessage);
            _jwtSecurityTokenHandlerProvider.ValidateTokenAsync(Arg.Any<string>(), Arg.Any<TokenValidationParameters>()).Throws(exception);

            Assert.False(await _tokenValidationService.ValidateToken(_httpRequest, CancellationToken.None));
            _logger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                Arg.Any<Exception>(),
                Arg.Is<string>(msg => msg == "Failed to validate the request Bearer token."));
        }

        [Fact]
        public async Task GivenValidateTokenAsyncReturnsInvalidTokenValidationResult_WhenAuthenticateTokenIsCalled_ReturnsFalse()
        {
            SetupSubstitutes();
            SetupHttpRequest(ExpectedToken);

            TokenValidationResult tvr = new TokenValidationResult() { IsValid = false };
            _jwtSecurityTokenHandlerProvider.ValidateTokenAsync(Arg.Any<string>(), Arg.Any<TokenValidationParameters>()).Returns(tvr);

            Assert.False(await _tokenValidationService.ValidateToken(_httpRequest, CancellationToken.None));
        }

        [Fact]
        public async Task GivenNoConditions_WhenAuthenticateTokenIsCalled_ReturnsTrue()
        {
            SetupSubstitutes();
            SetupHttpRequest(ExpectedToken);

            Assert.True(await _tokenValidationService.ValidateToken(_httpRequest, CancellationToken.None));
        }

        private void SetupSubstitutes()
        {
            // Issuers setup
            _issuers.Add(ExpectedIssuer, ExpectedMetadataEndpoint);
            _tokenValidationService.Issuers = _issuers;

            // AuthenticationConfiguration setup
            _authenticationConfiguration.IsAnonymousLoginEnabled = false;
            _authenticationConfiguration.Audience = ExpectedAudience;
            _metadataEndpoints.Add(ExpectedMetadataEndpoint);
            _authenticationConfiguration.TokenAuthorities.Returns(_metadataEndpoints);

            // OpenIdConfiguration retrieval setup
            _openIdConfiguration.Issuer = ExpectedIssuer;
            _openIdConfigurationProvider.GetConfigurationAsync(
                    Arg.Is<string>(str => str == ExpectedMetadataEndpoint),
                    Arg.Any<CancellationToken>()).Returns(_openIdConfiguration);

            // JwtSecurityToken setup
            _jwtSecurityToken = new JwtSecurityToken(issuer: ExpectedIssuer);
            _jwtSecurityTokenHandlerProvider.ReadJwtToken(Arg.Is<string>(str => str == ExpectedToken)).Returns(_jwtSecurityToken);

            // TokenValidationResult setup
            TokenValidationResult tvr = new TokenValidationResult() { IsValid = true };
            _jwtSecurityTokenHandlerProvider.ValidateTokenAsync(Arg.Any<string>(), Arg.Any<TokenValidationParameters>()).Returns(tvr);
        }

        private void SetupHttpRequest(string token)
        {
            if (token != string.Empty)
            {
                _httpRequest.Headers.TryGetValue(
                        Arg.Is<string>(str => str == HeaderNames.Authorization),
                        out Arg.Any<StringValues>()).
                    Returns(x =>
                    {
                        x[1] = new StringValues(TokenAuthScheme + token);
                        return true;
                    });
            }
            else
            {
                _httpRequest.Headers.TryGetValue(
                        Arg.Is<string>(str => str == HeaderNames.Authorization),
                        out Arg.Any<StringValues>()).
                    Returns(x =>
                    {
                        x[1] = new StringValues(TokenAuthScheme + token);
                        return false;
                    });
            }
        }
    }
}
