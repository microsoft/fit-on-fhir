// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IdentityModel.Tokens.Jwt;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Health.FitOnFhir.Common.Config;
using Microsoft.Health.FitOnFhir.Common.Services;
using Microsoft.Health.FitOnFhir.Common.Tests.Mocks;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.FitOnFhir.Common.Tests
{
    public class AuthStateServiceTests : AuthenticationBaseTests
    {
        private readonly AzureConfiguration _azureConfiguration;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly MockLogger<AuthStateService> _logger;
        private readonly AuthStateService _authStateService;

        public AuthStateServiceTests()
        {
            SetupConfiguration(false);

            // create new service for testing
            _azureConfiguration = Substitute.For<AzureConfiguration>();
            _blobServiceClient = Substitute.For<BlobServiceClient>();
            _logger = Substitute.For<MockLogger<AuthStateService>>();
            _authStateService = new AuthStateService(
                _azureConfiguration,
                AuthConfiguration,
                SecurityTokenHandlerProvider,
                _blobServiceClient,
                _logger);
        }

        [Fact]
        public void GivenAnonymousLoginEnabledWithValidRequest_WhenCreateAuthStateCalled_ReturnsAuthStateWithQueryParams()
        {
            SetupConfiguration(true);
            SetupHttpRequest(ExpectedToken, true);

            var authState = _authStateService.CreateAuthState(Request);

            Assert.Equal(ExpectedExternalSystem, authState.ExternalSystem);
            Assert.Equal(ExpectedExternalIdentifier, authState.ExternalIdentifier);
        }

        [Fact]
        public void GivenAnonymousLoginDisabledWithValidRequest_WhenCreateAuthStateCalled_ReturnsAuthStateWithSubjectAndIssuer()
        {
            SetupHttpRequest(ExpectedToken);

            var authState = _authStateService.CreateAuthState(Request);

            Assert.Equal(ExpectedIssuer, authState.ExternalSystem);
            Assert.Equal(ExpectedSubject, authState.ExternalIdentifier);
        }

        [Fact]
        public void GivenAnonymousLoginDisabledWithQueryParametersInRequest_WhenCreateAuthStateCalled_ThrowsArgumentException()
        {
            SetupHttpRequest(ExpectedToken, true);

            Assert.Throws<ArgumentException>(() => _authStateService.CreateAuthState(Request));

            _logger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                Arg.Is<string>(msg =>
                    msg == $"{Constants.ExternalIdQueryParameter} and {Constants.ExternalSystemQueryParameter} are forbidden query parameters with non-anonymous authorization."));
        }

        [Fact]
        public void GivenHttpRequestHasInvalidToken_WhenValidateTokenIsCalled_ReturnsDefaultAndErrorIsLogged()
        {
            SetupHttpRequest(string.Empty);

            var authState = _authStateService.CreateAuthState(Request);

            Assert.Null(authState);
            _logger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                Arg.Is<string>(msg => msg == "The request Authorization header is invalid."));
        }

        [Fact]
        public void GivenReadJwtTokenReturnsDefault_WhenValidateTokenIsCalled_ReturnsDefaultAndErrorIsLogged()
        {
            SetupHttpRequest(ExpectedToken);

            JwtSecurityToken jwtSecurityToken = default;
            SecurityTokenHandlerProvider.ReadJwtToken(Arg.Is<string>(str => str == ExpectedToken)).Returns(jwtSecurityToken);

            var authState = _authStateService.CreateAuthState(Request);

            Assert.Null(authState);
        }
    }
}
