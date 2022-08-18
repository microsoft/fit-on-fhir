// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Health.FitOnFhir.Common.Exceptions;
using Microsoft.Health.FitOnFhir.Common.Models;
using Microsoft.Health.FitOnFhir.Common.Repositories;
using Microsoft.Health.FitOnFhir.Common.Tests.Mocks;
using Microsoft.Health.FitOnFhir.GoogleFit.Client;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Models;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Responses;
using Microsoft.Health.FitOnFhir.GoogleFit.Common;
using Microsoft.Health.FitOnFhir.GoogleFit.Repositories;
using Microsoft.Health.FitOnFhir.GoogleFit.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Tests
{
    public class GoogleFitDataImporterTests
    {
        private readonly Guid _userGuid = Guid.NewGuid();
        private readonly string _userId;
        private const string _googleUserId = "me";
        private readonly CancellationToken _cancellationToken = CancellationToken.None;
        private readonly User _user;
        private readonly GoogleFitUser _googleFitUser;
        private readonly DataSourcesListResponse _dataSourcesListResponse = new DataSourcesListResponse() { DataSources = new List<DataSource>() };
        private readonly AuthTokensResponse _tokensResponse = Data.GetAuthTokensResponse();

        private readonly IGoogleFitImportService _googleFitImportService;
        private readonly IUsersTableRepository _usersTableRepository;
        private readonly IGoogleFitUserTableRepository _googleFitUserTableRepository;
        private readonly IGoogleFitClient _googleFitClient;
        private readonly MockLogger<GoogleFitDataImporter> _dataImporterLogger;
        private readonly IGoogleFitDataImporter _googleFitDataImporter;
        private readonly Func<DateTimeOffset> _utcNowFunc;

        private readonly IGoogleFitTokensService _googleFitTokensService;

        public GoogleFitDataImporterTests()
        {
            _googleFitUser = new GoogleFitUser(_googleUserId);

            _userId = _userGuid.ToString();
            _user = new User(_userGuid);
            _user.AddPlatformUserInfo(new PlatformUserInfo(GoogleFitConstants.GoogleFitPlatformName, _googleUserId, DataImportState.ReadyToImport));

            // GoogleFitDataImporter dependencies
            _usersTableRepository = Substitute.For<IUsersTableRepository>();
            _googleFitUserTableRepository = Substitute.For<IGoogleFitUserTableRepository>();
            _googleFitClient = Substitute.For<IGoogleFitClient>();
            _dataImporterLogger = Substitute.For<MockLogger<GoogleFitDataImporter>>();
            _googleFitImportService = Substitute.For<IGoogleFitImportService>();
            _googleFitTokensService = Substitute.For<IGoogleFitTokensService>();

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

            _usersTableRepository.Update(
                    Arg.Any<User>(),
                    Arg.Any<Func<User, User, User>>(),
                    Arg.Is<CancellationToken>(token => token == _cancellationToken)).
                Returns(
                    x =>
                    {
                        var user = new User(_userGuid);
                        user.AddPlatformUserInfo(new PlatformUserInfo(GoogleFitConstants.GoogleFitPlatformName, _googleUserId, DataImportState.Importing));
                        return user;
                    }, x =>
                    {
                        var user = new User(_userGuid);
                        user.AddPlatformUserInfo(new PlatformUserInfo(GoogleFitConstants.GoogleFitPlatformName, _googleUserId, DataImportState.Unauthorized));
                        return user;
                    });

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

            Received.InOrder(async () =>
            {
                await _usersTableRepository.Update(
                    Arg.Is<User>(usr => IsExpected(usr, DataImportState.Importing)),
                    Arg.Any<Func<User, User, User>>(),
                    Arg.Is<CancellationToken>(token => token == _cancellationToken));

                await _usersTableRepository.Update(
                    Arg.Is<User>(usr => IsExpected(usr, DataImportState.Unauthorized)),
                    Arg.Any<Func<User, User, User>>(),
                    Arg.Is<CancellationToken>(token => token == _cancellationToken));
            });

            await _googleFitClient.DidNotReceive().DataSourcesListRequest(
                Arg.Is<string>(access => access == Data.AccessToken),
                Arg.Is<CancellationToken>(cancel => cancel == _cancellationToken));

            await _googleFitImportService.DidNotReceive().ProcessDatasetRequests(
                Arg.Is<GoogleFitUser>(user => user.Id == _googleUserId),
                Arg.Is<IEnumerable<DataSource>>(list => list == _dataSourcesListResponse.DataSources),
                Arg.Is<AuthTokensResponse>(tknrsp => tknrsp == _tokensResponse),
                Arg.Is<CancellationToken>(cancel => cancel == _cancellationToken));

            await _googleFitUserTableRepository.DidNotReceive().Update(
                Arg.Is<GoogleFitUser>(usr => usr == _googleFitUser),
                Arg.Any<Func<GoogleFitUser, GoogleFitUser, GoogleFitUser>>(),
                Arg.Is<CancellationToken>(token => token == _cancellationToken));
        }

        [Fact]
        public async Task GivenAuthTokenResponseIsNull_WhenImportIsCalled_ImportOperationIsSkipped()
        {
            SetupMockSuccessReturns();
            _googleFitTokensService.RefreshToken(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<AuthTokensResponse>(null));

            await _googleFitDataImporter.Import(_userId, _googleUserId, _cancellationToken);

            await _googleFitClient.DidNotReceive().DataSourcesListRequest(Arg.Any<string>(), Arg.Any<CancellationToken>());
            await _googleFitImportService.DidNotReceive().ProcessDatasetRequests(Arg.Any<GoogleFitUser>(), Arg.Any<IEnumerable<DataSource>>(), Arg.Any<AuthTokensResponse>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GivenAuthTokenResponseIsNull_WhenImportIsCalled_UserImportStateIsSetToReadyToImport()
        {
            SetupMockSuccessReturns();
            _googleFitTokensService.RefreshToken(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<AuthTokensResponse>(null));

            await _googleFitDataImporter.Import(_userId, _googleUserId, _cancellationToken);

            await _usersTableRepository.Update(Arg.Is<User>(x => IsExpected(x, DataImportState.ReadyToImport)), Arg.Any<Func<User, User, User>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GivenAuthTokensResponseIsValid_WhenImportIsCalled_DataSourcesListRequestIsCalled()
        {
            SetupMockSuccessReturns();

            await _googleFitDataImporter.Import(_userId, _googleUserId, _cancellationToken);

            await _googleFitClient.Received(1).DataSourcesListRequest(
                Arg.Is<string>(access => access == Data.AccessToken),
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
                Arg.Is<AuthTokensResponse>(tknrsp => tknrsp.RefreshToken == Data.RefreshToken && tknrsp.AccessToken == Data.AccessToken),
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
                Arg.Any<Func<GoogleFitUser, GoogleFitUser, GoogleFitUser>>(),
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

            Received.InOrder(async () =>
            {
                await _usersTableRepository.Update(
                    Arg.Is<User>(usr => IsExpected(usr, DataImportState.Importing)),
                    Arg.Any<Func<User, User, User>>(),
                    Arg.Is<CancellationToken>(token => token == _cancellationToken));

                await _usersTableRepository.Update(
                    Arg.Is<User>(usr => IsExpected(usr, DataImportState.ReadyToImport)),
                    Arg.Any<Func<User, User, User>>(),
                    Arg.Is<CancellationToken>(token => token == _cancellationToken));
            });
        }

        [Fact]
        public async Task GivenImportServiceThrows_WhenImportIsCalled_GoogleUserIsNotUpdated()
        {
            SetupMockSuccessReturns();

            _googleFitImportService.ProcessDatasetRequests(
                _googleFitUser,
                Arg.Any<IEnumerable<DataSource>>(),
                _tokensResponse,
                Arg.Any<CancellationToken>()).Returns(x => throw new Exception());

            await _googleFitDataImporter.Import(_userId, _googleUserId, _cancellationToken);

            await _googleFitUserTableRepository.DidNotReceive().Update(
                Arg.Any<GoogleFitUser>(),
                Arg.Any<Func<GoogleFitUser, GoogleFitUser, GoogleFitUser>>(),
                Arg.Any<CancellationToken>());
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

            _usersTableRepository.Update(
                    Arg.Any<User>(),
                    Arg.Any<Func<User, User, User>>(),
                    Arg.Is<CancellationToken>(token => token == _cancellationToken)).
                Returns(
                    x =>
                {
                    var user = new User(_userGuid);
                    user.AddPlatformUserInfo(new PlatformUserInfo(GoogleFitConstants.GoogleFitPlatformName, _googleUserId, DataImportState.Importing));
                    return user;
                }, x =>
                    {
                        var user = new User(_userGuid);
                        user.AddPlatformUserInfo(new PlatformUserInfo(GoogleFitConstants.GoogleFitPlatformName, _googleUserId, DataImportState.ReadyToImport));
                        return user;
                    });
        }

        private bool IsExpected(User user, DataImportState dataImportState)
        {
            return string.Equals(_userId, user.Id, StringComparison.OrdinalIgnoreCase) &&
                   user.GetPlatformUserInfo().Any(x =>
                   {
                       return string.Equals(GoogleFitConstants.GoogleFitPlatformName, x.PlatformName, StringComparison.OrdinalIgnoreCase) &&
                              string.Equals(_googleUserId, x.UserId, StringComparison.OrdinalIgnoreCase) &&
                              string.Equals(dataImportState.ToString(), x.ImportState.ToString(), StringComparison.OrdinalIgnoreCase);
                   });
        }
    }
}
