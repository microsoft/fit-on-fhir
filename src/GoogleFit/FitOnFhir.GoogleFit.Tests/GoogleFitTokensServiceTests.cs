// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Health.FitOnFhir.Common.Repositories;
using Microsoft.Health.FitOnFhir.Common.Tests.Mocks;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Responses;
using Microsoft.Health.FitOnFhir.GoogleFit.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Tests
{
    public class GoogleFitTokensServiceTests
    {
        private const string _googleUserId = "me";
        private const string _accessToken = "AccessToken";
        private const string _refreshToken = "RefreshToken";
        private readonly AuthTokensResponse _tokensResponse = new AuthTokensResponse() { AccessToken = _accessToken, RefreshToken = _refreshToken };
        private readonly CancellationToken _cancellationToken = CancellationToken.None;

        private readonly IUsersKeyVaultRepository _usersKeyvaultRepository;
        private readonly IGoogleFitAuthService _googleFitAuthService;
        private readonly MockLogger<GoogleFitTokensService> _tokensServiceLogger;
        private readonly GoogleFitTokensService _googleFitTokensService;

        public GoogleFitTokensServiceTests()
        {
            // GoogleFitTokensService dependencies
            _usersKeyvaultRepository = Substitute.For<IUsersKeyVaultRepository>();
            _googleFitAuthService = Substitute.For<IGoogleFitAuthService>();
            _tokensServiceLogger = Substitute.For<MockLogger<GoogleFitTokensService>>();

            // create the service
            _googleFitTokensService = new GoogleFitTokensService(_googleFitAuthService, _usersKeyvaultRepository, _tokensServiceLogger);
        }

        [Fact]
        public async Task GivenValidGoogleFitId_WhenRefreshTokenIsCalled_GetByNameIsCalled()
        {
            SetupMockSuccessReturns();

            await _googleFitTokensService.RefreshToken(_googleUserId, _cancellationToken);

            await _usersKeyvaultRepository.Received(1).GetByName(
                Arg.Is<string>(userid => userid == _googleUserId),
                Arg.Is<CancellationToken>(cancel => cancel == _cancellationToken));
        }

        [Fact]
        public async Task GivenGetByNameReturnsRefreshToken_WhenRefreshTokenIsCalled_RefreshTokensRequestIsCalled()
        {
            SetupMockSuccessReturns();

            await _googleFitTokensService.RefreshToken(_googleUserId, _cancellationToken);

            await _googleFitAuthService.Received(1).RefreshTokensRequest(
                Arg.Is<string>(refresh => refresh == _refreshToken),
                Arg.Is<CancellationToken>(cancel => cancel == _cancellationToken));
        }

        [Fact]
        public async Task GivenRefreshTokensRequestReturnsAuthTokensResponse_WhenRefreshTokenIsCalled_UpsertIsCalled()
        {
            SetupMockSuccessReturns();

            await _googleFitTokensService.RefreshToken(_googleUserId, _cancellationToken);

            await _usersKeyvaultRepository.Received(1).Upsert(
                Arg.Is<string>(userid => userid == _googleUserId),
                Arg.Is<string>(refresh => refresh == _refreshToken),
                Arg.Is<CancellationToken>(cancel => cancel == _cancellationToken));
        }

        [Fact]
        public async Task GivenRefreshTokensRequestReturnsEmptyAuthTokensResponse_WhenRefreshTokenIsCalled_RefreshTokenLogsAnEmptyToken()
        {
            SetupMockSuccessReturns();

            AuthTokensResponse emptyTokensResponse = new AuthTokensResponse() { AccessToken = _accessToken, RefreshToken = string.Empty };

            // override the RefreshTokensRequest call
            _googleFitAuthService.RefreshTokensRequest(
                Arg.Is<string>(refresh => refresh == _refreshToken), Arg.Is<CancellationToken>(token => token == _cancellationToken)).Returns(emptyTokensResponse);

            await _googleFitTokensService.RefreshToken(_googleUserId, _cancellationToken);

            _tokensServiceLogger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Information),
                Arg.Is<string>(msg => msg.StartsWith($"RefreshToken is empty for {_googleUserId}")));
        }

        [Fact]
        public async Task GivenRefreshTokensRequestReturnsNullAuthTokensResponse_WhenRefreshTokenIsCalled_RefreshTokenLogsAnEmptyToken()
        {
            SetupMockSuccessReturns();

            AuthTokensResponse emptyTokensResponse = new AuthTokensResponse() { AccessToken = _accessToken, RefreshToken = null };

            // override the RefreshTokensRequest call
            _googleFitAuthService.RefreshTokensRequest(
                Arg.Is<string>(refresh => refresh == _refreshToken), Arg.Is<CancellationToken>(token => token == _cancellationToken)).Returns(emptyTokensResponse);

            await _googleFitTokensService.RefreshToken(_googleUserId, _cancellationToken);

            _tokensServiceLogger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Information),
                Arg.Is<string>(msg => msg.StartsWith($"RefreshToken is empty for {_googleUserId}")));
        }

        [Fact]
        public async Task GivenGetByNameThrowsException_WhenRefreshTokenIsCalled_RefreshTokenLogsATokenRefreshException()
        {
            string exceptionMessage = "retrieve token exception";
            var exception = new Exception(exceptionMessage);

            _usersKeyvaultRepository.GetByName(
                Arg.Is<string>(userid => userid == _googleUserId),
                Arg.Is<CancellationToken>(token => token == _cancellationToken)).Throws(exception);

            await _googleFitTokensService.RefreshToken(_googleUserId, _cancellationToken);

            _tokensServiceLogger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                Arg.Is<Exception>(ex => ex == exception),
                Arg.Is<string>(msg => msg == exceptionMessage));
        }

        [Fact]
        public async Task GivenRetrieveRefreshTokenThrowsException_WhenRefreshTokenIsCalled_RefreshTokenLogsATokenRefreshException()
        {
            string exceptionMessage = "retrieve token exception";
            var exception = new Exception(exceptionMessage);

            SetupMockSuccessReturns();

            _googleFitAuthService.RefreshTokensRequest(
                Arg.Is<string>(refresh => refresh == _refreshToken),
                Arg.Is<CancellationToken>(token => token == _cancellationToken)).Throws(exception);

            await _googleFitTokensService.RefreshToken(_googleUserId, _cancellationToken);

            _tokensServiceLogger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                Arg.Is<Exception>(ex => ex == exception),
                Arg.Is<string>(msg => msg == exceptionMessage));
        }

        [Fact]
        public async Task GivenUpsertThrowsException_WhenRefreshTokenIsCalled_RefreshTokenLogsATokenRefreshException()
        {
            string exceptionMessage = "retrieve token exception";
            var exception = new Exception(exceptionMessage);

            SetupMockSuccessReturns();

            _usersKeyvaultRepository.Upsert(
                Arg.Is<string>(userid => userid == _googleUserId),
                Arg.Is<string>(val => val == _tokensResponse.RefreshToken),
                Arg.Is<CancellationToken>(token => token == _cancellationToken)).Throws(exception);

            await _googleFitTokensService.RefreshToken(_googleUserId, _cancellationToken);

            _tokensServiceLogger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                Arg.Is<Exception>(ex => ex == exception),
                Arg.Is<string>(msg => msg == exceptionMessage));
        }

        private void SetupMockSuccessReturns()
        {
            _usersKeyvaultRepository.GetByName(
                Arg.Is<string>(userid => userid == _googleUserId),
                Arg.Is<CancellationToken>(token => token == _cancellationToken)).Returns(_refreshToken);

            _googleFitAuthService.RefreshTokensRequest(
                Arg.Is<string>(refresh => refresh == _refreshToken),
                Arg.Is<CancellationToken>(token => token == _cancellationToken)).Returns(_tokensResponse);
        }
    }
}
