// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.Common.Exceptions;
using FitOnFhir.Common.Models;
using FitOnFhir.Common.Repositories;
using FitOnFhir.Common.Tests.Mocks;
using FitOnFhir.GoogleFit.Client;
using FitOnFhir.GoogleFit.Client.Models;
using FitOnFhir.GoogleFit.Client.Responses;
using FitOnFhir.GoogleFit.Common;
using FitOnFhir.GoogleFit.Repositories;
using FitOnFhir.GoogleFit.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace FitOnFhir.GoogleFit.Tests
{
    public class GoogleFitDataImporterTests
    {
        private readonly string _userId = Guid.NewGuid().ToString();
        private const string _googleUserId = "me";
        private const string _accessToken = "AccessToken";
        private const string _refreshToken = "RefreshToken";
        private readonly CancellationToken _cancellationToken = CancellationToken.None;
        private readonly User _user = new User(Guid.NewGuid());
        private readonly GoogleFitUser _googleFitUser;
        private readonly DataSourcesListResponse _dataSourcesListResponse = new DataSourcesListResponse() { DataSources = new List<DataSource>() };
        private readonly AuthTokensResponse _tokensResponse = new AuthTokensResponse() { AccessToken = _accessToken, RefreshToken = _refreshToken };

        private readonly IGoogleFitImportService _googleFitImportService;
        private readonly IUsersTableRepository _usersTableRepository;
        private readonly IGoogleFitUserTableRepository _googleFitUserTableRepository;
        private readonly IGoogleFitClient _googleFitClient;
        private readonly MockLogger<GoogleFitDataImporter> _dataImporterLogger;
        private IGoogleFitDataImporter _googleFitDataImporter;
        private readonly Func<DateTimeOffset> _utcNowFunc;

        private readonly IGoogleFitAuthService _googleFitAuthService;
        private readonly IUsersKeyVaultRepository _usersKeyVaultRepository;
        private readonly IGoogleFitTokensService _googleFitTokensService;

        public GoogleFitDataImporterTests()
        {
            _googleFitUser = new GoogleFitUser(_googleUserId);
            _user.AddPlatformUserInfo(new PlatformUserInfo(GoogleFitConstants.GoogleFitPlatformName, _googleUserId, DataImportState.ReadyToSync));

            // GoogleFitDataImporter dependencies
            _usersTableRepository = Substitute.For<IUsersTableRepository>();
            _googleFitUserTableRepository = Substitute.For<IGoogleFitUserTableRepository>();
            _googleFitClient = Substitute.For<IGoogleFitClient>();
            _dataImporterLogger = Substitute.For<MockLogger<GoogleFitDataImporter>>();
            _googleFitImportService = Substitute.For<IGoogleFitImportService>();
            _googleFitTokensService = Substitute.For<IGoogleFitTokensService>();
            _googleFitAuthService = Substitute.For<IGoogleFitAuthService>();
            _usersKeyVaultRepository = Substitute.For<IUsersKeyVaultRepository>();

            _utcNowFunc = Substitute.For<Func<DateTimeOffset>>();
            var now = DateTimeOffset.Parse("01/12/2004");
            _utcNowFunc().Returns(now);

            // create the service
            _googleFitDataImporter = new GoogleFitDataImporter(
                _usersTableRepository,
                _googleFitUserTableRepository,
                _googleFitClient,
                _googleFitImportService,
                _googleFitTokensService,
                _utcNowFunc,
                _dataImporterLogger);
        }

        [Fact]
        public async Task GivenRefreshTokenThrowsTokenRefreshException_WhenImportIsCalled_ImportReturns()
        {
            _usersTableRepository.GetById(
                Arg.Is<string>(userid => userid == _userId),
                Arg.Is<CancellationToken>(token => token == _cancellationToken)).Returns(_user);

            // override ProcessDatasetRequests to throw an exception
            string exceptionMessage = "token refresh exception";
            var exception = new TokenRefreshException(exceptionMessage);
            _googleFitTokensService.RefreshToken(
                Arg.Is<string>(userid => userid == _googleUserId),
                Arg.Is<CancellationToken>(token => token == _cancellationToken)).Throws(exception);

            await _googleFitDataImporter.Import(_userId, _googleUserId, _cancellationToken);

            await _usersTableRepository.Received(1).GetById(
                Arg.Is<string>(usr => usr == _userId),
                Arg.Is<CancellationToken>(cancel => cancel == _cancellationToken));

            Assert.Equal(DataImportState.Unauthorized, _user.GetPlatformUserInfo().First().ImportState);

            await _googleFitClient.DidNotReceive().DataSourcesListRequest(
                Arg.Is<string>(access => access == _accessToken),
                Arg.Is<CancellationToken>(cancel => cancel == _cancellationToken));

            await _googleFitImportService.DidNotReceive().ProcessDatasetRequests(
                Arg.Is<GoogleFitUser>(user => user.Id == _googleUserId),
                Arg.Is<IEnumerable<DataSource>>(list => list == _dataSourcesListResponse.DataSources),
                Arg.Is<AuthTokensResponse>(tknrsp => tknrsp == _tokensResponse),
                Arg.Is<CancellationToken>(cancel => cancel == _cancellationToken));

            await _googleFitUserTableRepository.DidNotReceive().Update(
                Arg.Is<GoogleFitUser>(usr => usr == _googleFitUser),
                Arg.Is<CancellationToken>(token => token == _cancellationToken));

            await _usersTableRepository.DidNotReceive().Update(
                Arg.Is<User>(usr => usr == _user),
                Arg.Is<CancellationToken>(token => token == _cancellationToken));
        }

        [Fact]
        public async Task GivenAuthTokensResponseIsValid_WhenImportIsCalled_DataSourcesListRequestIsCalled()
        {
            SetupMockSuccessReturns();

            await _googleFitDataImporter.Import(_userId, _googleUserId, _cancellationToken);

            await _googleFitClient.Received(1).DataSourcesListRequest(
                Arg.Is<string>(access => access == _accessToken),
                Arg.Is<CancellationToken>(cancel => cancel == _cancellationToken));
        }

        [Fact]
        public async Task GivenAuthTokensResponseIsValid_WhenImportIsCalled_IGoogleFitUserTableRepositoryGetByIdIsCalled()
        {
            SetupMockSuccessReturns();

            await _googleFitDataImporter.Import(_userId, _googleUserId, _cancellationToken);

            await _googleFitUserTableRepository.Received(1).GetById(
                Arg.Is<string>(userid => userid == _googleUserId),
                Arg.Is<CancellationToken>(token => token == _cancellationToken));
        }

        [Fact]
        public async Task GivenAuthTokensResponseIsValid_WhenImportIsCalled_ProcessDatasetRequestsIsCalled()
        {
            SetupMockSuccessReturns();

            await _googleFitDataImporter.Import(_userId, _googleUserId, _cancellationToken);

            await _googleFitImportService.Received(1).ProcessDatasetRequests(
                Arg.Is<GoogleFitUser>(usr => usr.Id == _googleUserId),
                Arg.Is<IEnumerable<DataSource>>(list => list == _dataSourcesListResponse.DataSources),
                Arg.Is<AuthTokensResponse>(tknrsp => tknrsp.RefreshToken == _refreshToken && tknrsp.AccessToken == _accessToken),
                Arg.Is<CancellationToken>(cancel => cancel == _cancellationToken));
        }

        [Fact]
        public async Task GivenProcessDatasetRequestsThrowsException_WhenImportIsCalled_ImportLogsAnError()
        {
            SetupMockSuccessReturns();

            // override ProcessDatasetRequests to throw an exception
            string exceptionMessage = "process dataset exception";
            var exception = new Exception(exceptionMessage);
            _ = _googleFitImportService.ProcessDatasetRequests(
                Arg.Any<GoogleFitUser>(),
                Arg.Any<IEnumerable<DataSource>>(),
                Arg.Any<AuthTokensResponse>(),
                Arg.Any<CancellationToken>()).Throws(exception);

            await _googleFitDataImporter.Import(_userId, _googleUserId, _cancellationToken);

            _dataImporterLogger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                Arg.Is<Exception>(exc => exc == exception),
                Arg.Is<string>(msg => msg == exceptionMessage));
        }

        [Fact]
        public async Task GivenAuthTokensResponseIsValid_WhenImportIsCalled_IGoogleFitUserTableRepositoryUpdateIsCalled()
        {
            SetupMockSuccessReturns();

            await _googleFitDataImporter.Import(_userId, _googleUserId, _cancellationToken);

            await _googleFitUserTableRepository.Received(1).Update(
                Arg.Is<GoogleFitUser>(usr => usr == _googleFitUser),
                Arg.Is<CancellationToken>(token => token == _cancellationToken));
        }

        [Fact]
        public async Task GivenAuthTokensResponseIsValid_WhenImportIsCalled_IUsersTableRepositoryGetByIdIsCalled()
        {
            SetupMockSuccessReturns();

            await _googleFitDataImporter.Import(_userId, _googleUserId, _cancellationToken);

            await _usersTableRepository.Received(1).GetById(
                Arg.Is<string>(usr => usr == _userId),
                Arg.Is<CancellationToken>(cancel => cancel == _cancellationToken));
        }

        [Fact]
        public async Task GivenAuthTokensResponseIsValid_WhenImportIsCalled_UserLastSyncIsUpdated()
        {
            SetupMockSuccessReturns();

            await _googleFitDataImporter.Import(_userId, _googleUserId, _cancellationToken);

            Assert.Equal(_user.LastTouched, _utcNowFunc());
        }

        [Fact]
        public async Task GivenAuthTokensResponseIsValid_WhenImportIsCalled_IUsersTableRepositoryUpdateIsCalled()
        {
            SetupMockSuccessReturns();

            await _googleFitDataImporter.Import(_userId, _googleUserId, _cancellationToken);

            await _usersTableRepository.Received(1).Update(
                Arg.Is<User>(usr => usr == _user),
                Arg.Is<CancellationToken>(token => token == _cancellationToken));
        }

        [Fact]
        public async Task GivenAuthTokensResponseIsValid_WhenImportIsCalled_ImportStateIsSetToReadyToSync()
        {
            SetupMockSuccessReturns();

            await _googleFitDataImporter.Import(_userId, _googleUserId, _cancellationToken);

            Assert.Equal(DataImportState.ReadyToSync, _user.GetPlatformUserInfo().First().ImportState);
        }

        private void SetupMockSuccessReturns()
        {
            _googleFitTokensService.RefreshToken(
                Arg.Is<string>(userid => userid == _googleUserId),
                Arg.Is<CancellationToken>(token => token == _cancellationToken)).Returns(_tokensResponse);

            _googleFitClient.DataSourcesListRequest(
              Arg.Is<string>(access => access == _tokensResponse.AccessToken),
              Arg.Is<CancellationToken>(token => token == _cancellationToken)).Returns(_dataSourcesListResponse);

            _googleFitUserTableRepository.GetById(
                Arg.Is<string>(userid => userid == _googleUserId),
                Arg.Is<CancellationToken>(token => token == _cancellationToken)).Returns(_googleFitUser);

            _usersTableRepository.GetById(
                Arg.Is<string>(userid => userid == _userId),
                Arg.Is<CancellationToken>(token => token == _cancellationToken)).Returns(_user);
        }
    }
}
