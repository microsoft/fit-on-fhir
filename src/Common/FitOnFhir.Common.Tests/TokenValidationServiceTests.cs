// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging;
using Microsoft.Health.FitOnFhir.Common.Services;
using Microsoft.Health.FitOnFhir.Common.Tests.Mocks;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.FitOnFhir.Common.Tests
{
    public class TokenValidationServiceTests : AuthenticationBaseTests
    {
        private readonly MockLogger<TokenValidationService> _logger;
        private readonly TokenValidationService _tokenValidationService;

        public TokenValidationServiceTests()
        {
            // setup configuration before constructing service,
            // so that CreateIssuerMapping() will populate _issuers with the correct substitute
            SetupConfiguration(false);

            // create new service for testing
            _logger = Substitute.For<MockLogger<TokenValidationService>>();
            _tokenValidationService = new TokenValidationService(
                OpenIdConfigurationProvider,
                AuthConfiguration,
                SecurityTokenHandlerProvider,
                _logger);
        }

        [Fact]
        public async Task GivenIsAnonymousLoginEnabledIsTrue_WhenValidateTokenIsCalled_ReturnsTrue()
        {
            SetupConfiguration(true);
            SetupHttpRequest(string.Empty);

            Assert.True(await _tokenValidationService.ValidateToken(Request, CancellationToken.None));
        }

        [Fact]
        public async Task GivenHttpRequestIsNull_WhenValidateTokenIsCalled_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _tokenValidationService.ValidateToken(null, CancellationToken.None));
        }

        [Fact]
        public async Task GivenHttpRequestHasInvalidToken_WhenValidateTokenIsCalled_ReturnsFalseAndErrorIsLogged()
        {
            SetupHttpRequest(string.Empty);

            Assert.False(await _tokenValidationService.ValidateToken(Request, CancellationToken.None));
            _logger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                Arg.Is<string>(msg => msg == "The request Authorization header is invalid."));
        }

        [Fact]
        public async Task GivenReadJwtTokenReturnsDefault_WhenValidateTokenIsCalled_ReturnsFalseAndErrorIsLogged()
        {
            SetupHttpRequest(ExpectedToken);

            JwtSecurityToken jwtSecurityToken = default;
            SecurityTokenHandlerProvider.ReadJwtToken(Arg.Is<string>(str => str == ExpectedToken)).Returns(jwtSecurityToken);

            Assert.False(await _tokenValidationService.ValidateToken(Request, CancellationToken.None));
        }

        [Fact]
        public async Task GivenNoMappedIssuerMatchesTokenIssuer_WhenValidateTokenIsCalled_ReturnsFalseAndMessageIsLogged()
        {
            SetupHttpRequest(ExpectedToken);

            // Set the Issuer returned to not match ExpectedIssuer
            OpenIdConfiguration.Issuer = "OtherIssuer";
            OpenIdConfigurationProvider.GetConfigurationAsync(
                Arg.Is<string>(str => str == ExpectedMetadataEndpoint),
                Arg.Any<CancellationToken>()).Returns(OpenIdConfiguration);

            // create new service for testing
            var service = new TokenValidationService(
                OpenIdConfigurationProvider,
                AuthConfiguration,
                SecurityTokenHandlerProvider,
                _logger);

            Assert.False(await service.ValidateToken(Request, CancellationToken.None));
            _logger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Information),
                Arg.Is<string>(msg => msg == $"Issuer {ExpectedIssuer} not found in the list of authorized identity providers."));
        }

        [Fact]
        public async Task GivenTokenValidationResultHasException_WhenValidateTokenIsCalled_ReturnsFalseAndErrorIsLogged()
        {
            SetupHttpRequest(ExpectedToken);

            string exceptionMessage = "Invalid Token";
            var exception = new SecurityTokenValidationException(exceptionMessage);
            var result = new TokenValidationResult
            {
                IsValid = false,
                Exception = exception,
            };

            SecurityTokenHandlerProvider.ValidateTokenAsync(Arg.Any<string>(), Arg.Any<TokenValidationParameters>()).Returns(Task.FromResult(result));

            Assert.False(await _tokenValidationService.ValidateToken(Request, CancellationToken.None));
            _logger.Received(1).Log(Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error), exception, exceptionMessage);
        }

        [Fact]
        public async Task GivenValidateTokenAsyncReturnsInvalidTokenValidationResult_WhenValidateTokenIsCalled_ReturnsFalse()
        {
            SetupHttpRequest(ExpectedToken);

            var tvr = new TokenValidationResult() { IsValid = false };
            SecurityTokenHandlerProvider.ValidateTokenAsync(Arg.Any<string>(), Arg.Any<TokenValidationParameters>()).Returns(tvr);

            Assert.False(await _tokenValidationService.ValidateToken(Request, CancellationToken.None));
        }

        [Fact]
        public async Task GivenNoConditions_WhenValidateTokenIsCalled_ReturnsTrue()
        {
            SetupHttpRequest(ExpectedToken);

            Assert.True(await _tokenValidationService.ValidateToken(Request, CancellationToken.None));
        }
    }
}
