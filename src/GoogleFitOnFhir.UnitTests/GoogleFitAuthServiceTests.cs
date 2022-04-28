// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using GoogleFitOnFhir.Clients.GoogleFit;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;
using GoogleFitOnFhir.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace GoogleFitOnFhir.UnitTests
{
    public class GoogleFitAuthServiceTests
    {
        private readonly ILogger<GoogleFitAuthService> _logger;
        private readonly GoogleFitClientContext _clientContext;
        private readonly IGoogleFitAuthUriRequest _googleFitAuthUriRequest;
        private readonly IGoogleFitAuthTokensRequest _googleFitAuthTokensRequest;
        private readonly IGoogleFitRefreshTokenRequest _googleFitRefreshTokensRequest;
        private readonly IGoogleFitAuthService _googleFitAuthService;

        private static string _fakeRedirectUri = "http://localhost";

        public GoogleFitAuthServiceTests()
        {
            _clientContext = Substitute.For<GoogleFitClientContext>("testId", "testSecret", _fakeRedirectUri);
            _googleFitAuthUriRequest = Substitute.For<IGoogleFitAuthUriRequest>();
            _googleFitAuthTokensRequest = Substitute.For<IGoogleFitAuthTokensRequest>();
            _googleFitRefreshTokensRequest = Substitute.For<IGoogleFitRefreshTokenRequest>();
            _logger = NullLogger<GoogleFitAuthService>.Instance;
            _googleFitAuthService = new GoogleFitAuthService(
                _logger,
                _clientContext,
                _googleFitAuthUriRequest,
                _googleFitAuthTokensRequest,
                _googleFitRefreshTokensRequest);
        }

        [Fact]
        public async Task GivenAuthRequestIsValid_WhenAuthUriRequestIsCalled_AuthUriResponseIsReturned()
        {
            _googleFitAuthUriRequest.ExecuteAsync(Arg.Any<CancellationToken>()).Returns(
                Task.FromResult<AuthUriResponse>(new AuthUriResponse() { Uri = _fakeRedirectUri }));

            var result = await _googleFitAuthService.AuthUriRequest(CancellationToken.None);
            Assert.IsType<AuthUriResponse>(result);

            var expectedResult = new AuthUriResponse() { Uri = _fakeRedirectUri };
            Assert.Equal(expectedResult.Uri, result.Uri);
        }

        [Fact]
        public async Task GivenAuthTokensRequestIsValid_WhenAuthTokensRequestIsCalled_AuthTokensResponseIsReturned()
        {
            _googleFitAuthTokensRequest.ExecuteAsync(Arg.Any<CancellationToken>()).Returns(
                Task.FromResult<AuthTokensResponse>(new AuthTokensResponse() { AccessToken = "accessToken", RefreshToken = "refreshToken" }));

            var result = await _googleFitAuthService.AuthTokensRequest("authCode", CancellationToken.None);
            Assert.IsType<AuthTokensResponse>(result);

            var expectedResult = new AuthTokensResponse() { AccessToken = "accessToken", RefreshToken = "refreshToken" };
            Assert.Equal(expectedResult.AccessToken, result.AccessToken);
            Assert.Equal(expectedResult.RefreshToken, result.RefreshToken);
        }

        [Fact]
        public async Task GivenRefreshTokensRequestIsValid_WhenRefreshTokensRequestIsCalled_AuthTokensResponseIsReturned()
        {
            _googleFitRefreshTokensRequest.ExecuteAsync(Arg.Any<CancellationToken>()).Returns(
                Task.FromResult<AuthTokensResponse>(new AuthTokensResponse() { AccessToken = "accessToken", RefreshToken = "refreshToken" }));

            var result = await _googleFitAuthService.RefreshTokensRequest("refreshToken", CancellationToken.None);
            Assert.IsType<AuthTokensResponse>(result);

            var expectedResult = new AuthTokensResponse() { AccessToken = "accessToken", RefreshToken = "refreshToken" };
            Assert.Equal(expectedResult.AccessToken, result.AccessToken);
            Assert.Equal(expectedResult.RefreshToken, result.RefreshToken);
        }
    }
}
