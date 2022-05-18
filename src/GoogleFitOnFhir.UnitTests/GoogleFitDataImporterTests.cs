// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitOnFhir.Common.Models;
using FitOnFhir.GoogleFit.Clients.GoogleFit;
using FitOnFhir.GoogleFit.Clients.GoogleFit.Models;
using FitOnFhir.GoogleFit.Clients.GoogleFit.Responses;
using FitOnFhir.GoogleFit.Common;
using FitOnFhir.GoogleFit.Repositories;
using FitOnFhir.GoogleFit.Services;
using GoogleFitOnFhir.UnitTests.Mocks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace GoogleFitOnFhir.UnitTests
{
    public class GoogleFitDataImporterTests
    {
        private const string _userId = "me";
        private const string _accessToken = "AccessToken";
        private const string _refreshToken = "RefreshToken";
        private readonly CancellationToken _cancellationToken = CancellationToken.None;
        private readonly User _user = new User(_userId, Constants.GoogleFitPlatformName);
        private readonly DataSourcesListResponse _dataSourcesListResponse = new DataSourcesListResponse() { DataSources = new List<DataSource>() };
        private readonly AuthTokensResponse _tokensResponse = new AuthTokensResponse() { AccessToken = _accessToken, RefreshToken = _refreshToken };

        private readonly IGoogleFitImportService _googleFitImportService;
        private readonly IGoogleFitAuthService _googleFitAuthService;
        private readonly IUsersTableRepository _usersTableRepository;
        private readonly IGoogleFitClient _googleFitClient;
        private readonly MockLogger<GoogleFitDataImporter> _dataImporterLogger;
        private readonly IUsersKeyVaultRepository _usersKeyvaultRepository;
        private readonly IGoogleFitDataImporter _googleFitDataImporter;
        private readonly Func<DateTimeOffset> _utcNowFunc;

        public GoogleFitDataImporterTests()
        {
            // GoogleFitDataImporter dependencies
            _usersTableRepository = Substitute.For<IUsersTableRepository>();
            _googleFitClient = Substitute.For<IGoogleFitClient>();
            _dataImporterLogger = Substitute.For<MockLogger<GoogleFitDataImporter>>();
            _usersKeyvaultRepository = Substitute.For<IUsersKeyVaultRepository>();
            _googleFitAuthService = Substitute.For<IGoogleFitAuthService>();
            _googleFitImportService = Substitute.For<IGoogleFitImportService>();
            _utcNowFunc = Substitute.For<Func<DateTimeOffset>>();
            var now = DateTimeOffset.Parse("01/12/2004");
            _utcNowFunc().Returns(now);

            // create the service
            _googleFitDataImporter = new GoogleFitDataImporter(
                _usersTableRepository,
                _googleFitClient,
                _googleFitImportService,
                _usersKeyvaultRepository,
                _googleFitAuthService,
                _utcNowFunc,
                _dataImporterLogger);
        }

        [Fact]
        public void GivenWhenUsersKeyVaultGetByNameThrowsException_WhenImportIsCalled_ThrowsException()
        {
            string exceptionMessage = "import exception";
            _usersKeyvaultRepository.GetByName(
                Arg.Is<string>(user => user == _userId),
                Arg.Is<CancellationToken>(cancel => cancel == _cancellationToken)).Throws(new Exception(exceptionMessage));

            Assert.ThrowsAsync<Exception>(async () => await _googleFitDataImporter.Import(_userId, _cancellationToken));
        }

        [Fact]
        public async Task GivenWhenUsersKeyVaultGetByNameThrowsAggregateException_WhenImportIsCalled_ImportLogsAnError()
        {
            string exceptionMessage = "import exception";
            var exception = new AggregateException(exceptionMessage);
            _usersKeyvaultRepository.GetByName(
                Arg.Is<string>(user => user == _userId),
                Arg.Is<CancellationToken>(cancel => cancel == _cancellationToken)).Throws(exception);

            await _googleFitDataImporter.Import(_userId, _cancellationToken);

            _dataImporterLogger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                Arg.Is<AggregateException>(exc => exc == exception),
                Arg.Is<string>(msg => msg == exceptionMessage));
        }

        [Fact]
        public async Task GivenWhenRefreshTokensRequestReturnsAuthTokensResponse_WhenImportIsCalled_UpsertIsCalled()
        {
            SetupMockSuccessReturns();

            await _googleFitDataImporter.Import(_userId, _cancellationToken);

            _ = _usersKeyvaultRepository.Received(1).Upsert(
                Arg.Is<string>(userid => userid == _userId),
                Arg.Is<string>(refresh => refresh == _refreshToken),
                Arg.Is<CancellationToken>(cancel => cancel == _cancellationToken));
        }

        [Fact]
        public async Task GivenWhenRefreshTokensRequestReturnsEmptyAuthTokensResponse_WhenImportIsCalled_ImportLogsAnEmptyToken()
        {
            SetupMockSuccessReturns();

            AuthTokensResponse emptyTokensResponse = new AuthTokensResponse() { AccessToken = _accessToken, RefreshToken = string.Empty };

            // override the RefreshTokensRequest call
            _googleFitAuthService.RefreshTokensRequest(
                Arg.Is<string>(refresh => refresh == _refreshToken), Arg.Is<CancellationToken>(token => token == _cancellationToken)).Returns(emptyTokensResponse);

            await _googleFitDataImporter.Import(_userId, _cancellationToken);

            _dataImporterLogger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Information),
                Arg.Is<string>(msg => msg.StartsWith($"RefreshToken is empty for {_userId}")));
        }

        [Fact]
        public async Task GivenWhenRefreshTokensRequestReturnsNullAuthTokensResponse_WhenImportIsCalled_ImportLogsAnEmptyToken()
        {
            SetupMockSuccessReturns();

            AuthTokensResponse emptyTokensResponse = new AuthTokensResponse() { AccessToken = _accessToken, RefreshToken = null };

            // override the RefreshTokensRequest call
            _googleFitAuthService.RefreshTokensRequest(
                Arg.Is<string>(refresh => refresh == _refreshToken), Arg.Is<CancellationToken>(token => token == _cancellationToken)).Returns(emptyTokensResponse);

            await _googleFitDataImporter.Import(_userId, _cancellationToken);

            _dataImporterLogger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Information),
                Arg.Is<string>(msg => msg.StartsWith($"RefreshToken is empty for {_userId}")));
        }

        [Fact]
        public async Task GivenWhenGetByNameReturnsRefreshToken_WhenImportIsCalled_RefreshTokensRequestIsCalled()
        {
            SetupMockSuccessReturns();

            await _googleFitDataImporter.Import(_userId, _cancellationToken);

            _ = _googleFitAuthService.Received(1).RefreshTokensRequest(
                Arg.Is<string>(refresh => refresh == _refreshToken),
                Arg.Is<CancellationToken>(cancel => cancel == _cancellationToken));
        }

        [Fact]
        public async Task GivenWhenAuthTokensResponseIsValid_WhenImportIsCalled_DataSourcesListRequestIsCalled()
        {
            SetupMockSuccessReturns();

            await _googleFitDataImporter.Import(_userId, _cancellationToken);

            _ = _googleFitClient.Received(1).DataSourcesListRequest(
                Arg.Is<string>(access => access == _accessToken),
                Arg.Is<CancellationToken>(cancel => cancel == _cancellationToken));
        }

        [Fact]
        public async Task GivenWhenAuthTokensResponseIsValid_WhenImportIsCalled_GetByIdIsCalled()
        {
            SetupMockSuccessReturns();

            await _googleFitDataImporter.Import(_userId, _cancellationToken);

            _ = _usersTableRepository.Received(1).GetById(
                Arg.Is<string>(usr => usr == _userId),
                Arg.Is<CancellationToken>(cancel => cancel == _cancellationToken));
        }

        [Fact]
        public async Task GivenWhenAuthTokensResponseIsValid_WhenImportIsCalled_UpdateIsCalled()
        {
            SetupMockSuccessReturns();

            await _googleFitDataImporter.Import(_userId, _cancellationToken);

            _ = _usersTableRepository.Received(1).Update(
                Arg.Is<User>(usr => usr == _user),
                Arg.Is<CancellationToken>(token => token == _cancellationToken));
        }

        [Fact]
        public async Task GivenWhenAuthTokensResponseIsValid_WhenImportIsCalled_UserLastSyncIsUpdated()
        {
            SetupMockSuccessReturns();

            await _googleFitDataImporter.Import(_userId, _cancellationToken);

            Assert.Equal(_user.LastSync, _utcNowFunc());
        }

        [Fact]
        public async Task GivenWhenProcessDatasetRequestsThrowsException_WhenImportIsCalled_ImportLogsAnError()
        {
            SetupMockSuccessReturns();

            // override ProcessDatasetRequests to throw an exception
            string exceptionMessage = "process dataset exception";
            var exception = new Exception(exceptionMessage);
            _ = _googleFitImportService.ProcessDatasetRequests(
                Arg.Any<User>(),
                Arg.Any<IEnumerable<DataSource>>(),
                Arg.Any<string>(),
                Arg.Any<AuthTokensResponse>(),
                Arg.Any<CancellationToken>()).Throws(exception);

            await _googleFitDataImporter.Import(_userId, _cancellationToken);

            _dataImporterLogger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                Arg.Is<Exception>(exc => exc == exception),
                Arg.Is<string>(msg => msg == exceptionMessage));
        }

        [Fact]
        public async Task GivenWhenUserLastSyncIsNull_WhenImportIsCalled_DatasetIdReflectsCorrectTimespan()
        {
            SetupMockSuccessReturns();

            // set start DateTimeOffset to 30 days prior to "now"
            var start = _utcNowFunc().AddDays(-30);
            var end = _utcNowFunc();
            var datasetId = GenerateDatasetId(start, end);

            await _googleFitDataImporter.Import(_userId, _cancellationToken);

            _ = _googleFitImportService.Received(1).ProcessDatasetRequests(
                Arg.Is<User>(usr => usr == _user),
                Arg.Is<IEnumerable<DataSource>>(list => list == _dataSourcesListResponse.DataSources),
                Arg.Is<string>(id => id == datasetId),
                Arg.Is<AuthTokensResponse>(tknrsp => tknrsp == _tokensResponse),
                Arg.Is<CancellationToken>(cancel => cancel == _cancellationToken));
        }

        [Fact]
        public async Task GivenWhenUserLastSyncNotNull_WhenImportIsCalled_DatasetIdReflectsLastSyncValue()
        {
            SetupMockSuccessReturns();

            // set User LastSync value to one day before mocked "now" returned by _utcNowFunc
            var lastSync = DateTimeOffset.Parse("01/11/2004");
            _user.LastSync = lastSync;

            var end = _utcNowFunc();
            var datasetId = GenerateDatasetId(lastSync, end);

            await _googleFitDataImporter.Import(_userId, _cancellationToken);

            _ = _googleFitImportService.Received(1).ProcessDatasetRequests(
                Arg.Is<User>(usr => usr == _user),
                Arg.Is<IEnumerable<DataSource>>(list => list == _dataSourcesListResponse.DataSources),
                Arg.Is<string>(id => id == datasetId),
                Arg.Is<AuthTokensResponse>(tknrsp => tknrsp == _tokensResponse),
                Arg.Is<CancellationToken>(cancel => cancel == _cancellationToken));
        }

        private void SetupMockSuccessReturns()
        {
            _usersKeyvaultRepository.GetByName(
              Arg.Is<string>(userid => userid == _userId),
              Arg.Is<CancellationToken>(token => token == _cancellationToken)).Returns(_refreshToken);

            _googleFitAuthService.RefreshTokensRequest(
               Arg.Is<string>(refresh => refresh == _refreshToken), Arg.Is<CancellationToken>(token => token == _cancellationToken)).Returns(_tokensResponse);

            _googleFitClient.DataSourcesListRequest(
              Arg.Is<string>(access => access == _tokensResponse.AccessToken),
              Arg.Is<CancellationToken>(token => token == _cancellationToken)).Returns(_dataSourcesListResponse);

            _usersTableRepository.GetById(
              Arg.Is<string>(userid => userid == _userId),
              Arg.Is<CancellationToken>(token => token == _cancellationToken)).Returns(_user);

            _ = _googleFitImportService.ProcessDatasetRequests(
            Arg.Is<User>(usr => usr == _user),
            Arg.Is<IEnumerable<DataSource>>(list => list == _dataSourcesListResponse.DataSources),
            Arg.Any<string>(),
            Arg.Is<AuthTokensResponse>(tknrsp => tknrsp == _tokensResponse),
            Arg.Is<CancellationToken>(cancel => cancel == _cancellationToken));

            _ = _usersTableRepository.Update(
                Arg.Is<User>(usr => usr == _user),
                Arg.Is<CancellationToken>(token => token == _cancellationToken));
        }

        private string GenerateDatasetId(DateTimeOffset start, DateTimeOffset end)
        {
            var startDate = start.ToUnixTimeMilliseconds() * 1000000;
            var endDate = end.ToUnixTimeMilliseconds() * 1000000;
            return startDate + "-" + endDate;
        }
    }
}
