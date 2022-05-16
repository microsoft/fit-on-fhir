// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Producer;
using GoogleFitOnFhir.Clients.GoogleFit;
using GoogleFitOnFhir.Clients.GoogleFit.Config;
using GoogleFitOnFhir.Clients.GoogleFit.Models;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;
using GoogleFitOnFhir.Common;
using GoogleFitOnFhir.Models;
using GoogleFitOnFhir.Repositories;
using GoogleFitOnFhir.Services;
using GoogleFitOnFhir.UnitTests.Mocks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Logging.Telemetry;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace GoogleFitOnFhir.UnitTests
{
    public class GoogleFitDataImporterTests
    {
        private const string _userId = "me";
        private const string _clientId = "clientId";
        private const string _clientSecret = "clientSecret";
        private const string _hostName = "http://localhost";
        private const string _accessToken = "AccessToken";
        private const string _refreshToken = "RefreshToken";
        private const string _dataSetId = "then-now";
        private readonly CancellationToken _cancellationToken = CancellationToken.None;

        private readonly IGoogleFitImportService _googleFitImportService;
        private readonly IGoogleFitAuthService _googleFitAuthService;
        private readonly IUsersTableRepository _usersTableRepository;
        private readonly IGoogleFitClient _googleFitClient;
        private MockLogger<GoogleFitDataImporter> _dataImporterLogger;
        private readonly IUsersKeyVaultRepository _usersKeyvaultRepository;
        private readonly IGoogleFitDataImporter _googleFitDataImporter;

        public GoogleFitDataImporterTests()
        {
            // GoogleFitDataImporter dependencies
            _usersTableRepository = Substitute.For<IUsersTableRepository>();
            _googleFitClient = Substitute.For<IGoogleFitClient>();
            _dataImporterLogger = Substitute.For<MockLogger<GoogleFitDataImporter>>();
            _usersKeyvaultRepository = Substitute.For<IUsersKeyVaultRepository>();
            _googleFitAuthService = Substitute.For<IGoogleFitAuthService>();
            _googleFitImportService = Substitute.For<IGoogleFitImportService>();

            // create the service
            _googleFitDataImporter = new GoogleFitDataImporter(
                _usersTableRepository,
                _googleFitClient,
                _googleFitImportService,
                _usersKeyvaultRepository,
                _googleFitAuthService,
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

        private void SetupMockSuccessReturns()
        {
            AuthTokensResponse tokensResponse = new AuthTokensResponse() { AccessToken = _accessToken, RefreshToken = _refreshToken };
            DataSourcesListResponse dataSourcesListResponse = new DataSourcesListResponse() { DataSources = new List<DataSource>() };
            User user = new User(_userId, Constants.GoogleFitPlatformName);

            _usersKeyvaultRepository.GetByName(
              Arg.Is<string>(userid => userid == _userId),
              Arg.Is<CancellationToken>(token => token == _cancellationToken)).Returns(_refreshToken);

            _googleFitAuthService.RefreshTokensRequest(
               Arg.Is<string>(refresh => refresh == _refreshToken), Arg.Is<CancellationToken>(token => token == _cancellationToken)).Returns(tokensResponse);

            _googleFitClient.DataSourcesListRequest(
              Arg.Is<string>(access => access == tokensResponse.AccessToken),
              Arg.Is<CancellationToken>(token => token == _cancellationToken)).Returns(dataSourcesListResponse);

            _usersTableRepository.GetById(
              Arg.Is<string>(userid => userid == _userId),
              Arg.Is<CancellationToken>(token => token == _cancellationToken)).Returns(user);

            _ = _googleFitImportService.ProcessDatasetRequests(
            Arg.Is<User>(usr => usr == user),
            Arg.Is<IEnumerable<DataSource>>(list => list == dataSourcesListResponse.DataSources),
            Arg.Any<string>(),
            Arg.Is<AuthTokensResponse>(tknrsp => tknrsp == tokensResponse),
            Arg.Is<CancellationToken>(cancel => cancel == _cancellationToken));

            _ = _usersTableRepository.Update(
                Arg.Is<User>(usr => usr == user),
                Arg.Is<CancellationToken>(token => token == _cancellationToken));
        }
    }
}
