// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.Common.Tests.Mocks;
using FitOnFhir.GoogleFit.Client;
using FitOnFhir.GoogleFit.Client.Config;
using FitOnFhir.GoogleFit.Client.Models;
using FitOnFhir.GoogleFit.Client.Responses;
using FitOnFhir.GoogleFit.Client.Telemetry;
using FitOnFhir.GoogleFit.Services;
using FitOnFhir.GoogleFit.Tests.Mocks;
using Google.Apis.Fitness.v1.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Common.Config;
using Microsoft.Health.Logging.Telemetry;
using NSubstitute;
using NSubstitute.Core;
using NSubstitute.ExceptionExtensions;
using Xunit;
using DataSource = FitOnFhir.GoogleFit.Client.Models.DataSource;

namespace FitOnFhir.GoogleFit.Tests
{
    public class GoogleFitImportServiceTests
    {
        private const string _googleUserId = "me";
        private const string _accessToken = "AccessToken";
        private const string _refreshToken = "RefreshToken";
        private const string _dataStreamId = "DataStreamId";
        private const string _deviceUid = "DeviceUid";
        private const string _applicationPackageName = "ApplicationPackageName";

        private readonly AuthTokensResponse _tokensResponse = new AuthTokensResponse() { AccessToken = _accessToken, RefreshToken = _refreshToken };
        private readonly CancellationToken _cancellationToken = CancellationToken.None;

        private readonly Dataset _dataset = new Dataset();
        private readonly DataSource _dataSource = new DataSource(_dataStreamId, _deviceUid, _applicationPackageName);
        private readonly List<DataSource> _dataSources = new List<DataSource>();

        private readonly DateTimeOffset _now =
            new DateTimeOffset(2004, 1, 12, 0, 0, 0, new TimeSpan(-5, 0, 0));

        private readonly long _lastSyncTime =
            new DateTimeOffset(2004, 1, 11, 11, 59, 59, new TimeSpan(-5, 0, 0)).ToUnixTimeMilliseconds() * 1000000;

        private readonly long _oneDayBack =
            new DateTimeOffset(2004, 1, 11, 0, 0, 0, new TimeSpan(-5, 0, 0)).ToUnixTimeMilliseconds() * 1000000;

        private readonly GoogleFitUser _googleFitUser;
        private readonly MockEventHubProducerClient _eventHubProducerClient;
        private readonly MockFaultyEventHubProducerClient _faultyEventHubProducerClient;
        private readonly MedTechDataset _medTechDataset;

        private readonly IGoogleFitClient _googleFitClient;
        private readonly GoogleFitImportOptions _options;
        private readonly Func<DateTimeOffset> _utcNowFunc;
        private readonly MockLogger<GoogleFitImportService> _importServiceLogger;
        private readonly ITelemetryLogger _telemetryLogger;
        private IGoogleFitImportService _googleFitImportService;

        public GoogleFitImportServiceTests()
        {
            _googleFitUser = Substitute.For<GoogleFitUser>(_googleUserId);

            _medTechDataset = Substitute.For<MedTechDataset>(_dataset, _dataSource);
            _dataSources.Add(_dataSource);
            _faultyEventHubProducerClient = new MockFaultyEventHubProducerClient();

            var parallelTaskOptions = new ParallelTaskOptions() { MaxConcurrency = 10 };
            _options = Substitute.For<GoogleFitImportOptions>();
            _options.ParallelTaskOptions.Returns(parallelTaskOptions);
            var telemetryLogger = new GoogleFitExceptionTelemetryProcessor();
            _options.ExceptionService.Returns(telemetryLogger);
            _options.HistoricalImportTimeSpan.Returns(TimeSpan.FromDays(30));

            // GoogleFitImportService dependencies
            _googleFitClient = Substitute.For<IGoogleFitClient>();
            _eventHubProducerClient = new MockEventHubProducerClient();
            _utcNowFunc = Substitute.For<Func<DateTimeOffset>>();
            _utcNowFunc().Returns(_now);
            _importServiceLogger = Substitute.For<MockLogger<GoogleFitImportService>>();
            _telemetryLogger = Substitute.For<ITelemetryLogger>();

            // create the service
            _googleFitImportService = new GoogleFitImportService(
                _googleFitClient,
                null,
                _eventHubProducerClient,
                _options,
                _utcNowFunc,
                _importServiceLogger,
                _telemetryLogger);
        }

        [Fact]
        public async Task GivenNoLastSyncStored_WhenTryGetLastSyncTime_GeneratesDataSetIdForLast30Days()
        {
            string thirtyDaysBackDataSetId = "1071291600000000000-1073883600000000000";

            await _googleFitImportService.ProcessDatasetRequests(_googleFitUser, _dataSources, _tokensResponse, _cancellationToken);

            _ = _googleFitClient.Received(1).DatasetRequest(
                Arg.Is<string>(access => access == _tokensResponse.AccessToken),
                Arg.Is<DataSource>(ds => ds == _dataSource),
                Arg.Is<string>(str => str == thirtyDaysBackDataSetId),
                Arg.Any<int>(),
                Arg.Is<CancellationToken>(token => token == _cancellationToken));
        }

        [Fact]
        public async Task GivenLastSyncStored_WhenTryGetLastSyncTime_GeneratesDataSetIdForLastSync()
        {
            string oneDayBackDataSetId = "1073797200000000000-1073883600000000000";

            _googleFitUser.TryGetLastSyncTime(_dataStreamId, out Arg.Any<long>())
                .Returns(x =>
                {
                    x[1] = _oneDayBack;
                    return true;
                });

            await _googleFitImportService.ProcessDatasetRequests(_googleFitUser, _dataSources, _tokensResponse, _cancellationToken);

            _ = _googleFitClient.Received(1).DatasetRequest(
                Arg.Is<string>(access => access == _tokensResponse.AccessToken),
                Arg.Is<DataSource>(ds => ds == _dataSource),
                Arg.Is<string>(str => str == oneDayBackDataSetId),
                Arg.Any<int>(),
                Arg.Is<CancellationToken>(token => token == _cancellationToken));
        }

        [Fact]
        public async Task GivenNoDataSetForDataSource_WhenDatasetRequestIsCalled_WorkerThreadContinues()
        {
            MedTechDataset nullMedTechDataset = null;
            string loggerMsg = $"No Dataset for: DataStreamId for user: {_googleUserId}";

            _googleFitClient.DatasetRequest(
                Arg.Is<string>(access => access == _tokensResponse.AccessToken),
                Arg.Any<DataSource>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Is<CancellationToken>(token => token == _cancellationToken)).Returns(nullMedTechDataset);

            await _googleFitImportService.ProcessDatasetRequests(_googleFitUser, _dataSources, _tokensResponse, _cancellationToken);

            _importServiceLogger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Information),
                Arg.Is<string>(msg => msg == loggerMsg));

            Assert.Equal(0, _eventHubProducerClient.CreateBatchAsyncCalls);
            Assert.Equal(0, _eventHubProducerClient.SendAsyncCalls);
            _googleFitUser.DidNotReceive().SaveLastSyncTime(Arg.Any<string>(), Arg.Any<long>());
        }

        [Fact]
        public async Task GivenEventDataBatchTryAddReturnsFalse_WhenMedTechDatasetAdded_EventBatchExceptionIsLogged()
        {
            string errorMessaage = $"Event data too large, Dataset: 0, User: {_dataStreamId}";

            // create a different service, with an EventHubProducerClient mock (_faultyEventHubProducerClient)
            // that uses a TryAdd callback override which returns false
            _googleFitImportService = new GoogleFitImportService(
                _googleFitClient,
                null,
                _faultyEventHubProducerClient,
                _options,
                _utcNowFunc,
                _importServiceLogger,
                _telemetryLogger);

            SetupMockSuccessReturns();

            await _googleFitImportService.ProcessDatasetRequests(_googleFitUser, _dataSources, _tokensResponse, _cancellationToken);

            _importServiceLogger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                Arg.Is<string>(msg => msg == errorMessaage));
        }

        [Fact]
        public void GivenDatasetRequestThrowsException_WhenProcessDatasetRequestsIsCalled_ExceptionIsThrown()
        {
            string exceptionMessage = "DatasetRequest exception";
            var datasetRequestException = new Exception(exceptionMessage);

            _googleFitUser.TryGetLastSyncTime(_dataStreamId, out Arg.Any<long>())
                .Returns(x =>
                {
                    x[1] = _oneDayBack;
                    return true;
                });

            _googleFitClient.DatasetRequest(
                Arg.Is<string>(access => access == _tokensResponse.AccessToken),
                Arg.Any<DataSource>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Is<CancellationToken>(token => token == _cancellationToken)).Throws(datasetRequestException);

            Assert.ThrowsAsync<Exception>(async () => await _googleFitImportService.ProcessDatasetRequests(_googleFitUser, _dataSources, _tokensResponse, _cancellationToken));
        }

        [Fact]
        public async Task GivenNextPageTokenIsNotNull_WhenProcessDatasetRequestsIsCalled_AllPageResultsAreRetrieved()
        {
            string nextPageToken = "next page";
            string nullPageToken = null;
            _medTechDataset.GetMaxEndTimeNanos().Returns(_lastSyncTime);
            _medTechDataset.GetPageToken().Returns(nextPageToken);

            _googleFitUser.TryGetLastSyncTime(_dataStreamId, out Arg.Any<long>())
                .Returns(x =>
                {
                    x[1] = _oneDayBack;
                    return true;
                });

            _medTechDataset.GetPageToken().Returns(nextPageToken);

            _googleFitClient.DatasetRequest(
                Arg.Is<string>(access => access == _tokensResponse.AccessToken),
                Arg.Any<DataSource>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Is<CancellationToken>(token => token == _cancellationToken),
                Arg.Is<string>(str => str == null)).Returns(_medTechDataset);

            MedTechDataset lastMedTechDataset = Substitute.For<MedTechDataset>(_dataset, _dataSource);
            lastMedTechDataset.GetMaxEndTimeNanos().Returns(_lastSyncTime);
            lastMedTechDataset.GetPageToken().Returns(nullPageToken);

            _googleFitClient.DatasetRequest(
                Arg.Is<string>(access => access == _tokensResponse.AccessToken),
                Arg.Any<DataSource>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Is<CancellationToken>(token => token == _cancellationToken),
                Arg.Is<string>(str => str == nextPageToken)).Returns(lastMedTechDataset);

            await _googleFitImportService.ProcessDatasetRequests(_googleFitUser, _dataSources, _tokensResponse, _cancellationToken);

            _googleFitUser.Received(1).TryGetLastSyncTime(Arg.Is<string>(str => str == _dataStreamId), out Arg.Any<long>());
            Assert.Equal(2, _eventHubProducerClient.CreateBatchAsyncCalls);
            Assert.Equal(2, _eventHubProducerClient.SendAsyncCalls);
            _googleFitUser.Received(2).SaveLastSyncTime(Arg.Is<string>(str => str == _dataStreamId), Arg.Is<long>(dto => dto == _lastSyncTime + 1));
        }

        [Fact]
        public async Task GivenNoConditions_WhenProcessDatasetRequestsIsCalled_Completes()
        {
            SetupMockSuccessReturns();

            await _googleFitImportService.ProcessDatasetRequests(_googleFitUser, _dataSources, _tokensResponse, _cancellationToken);

            _googleFitUser.Received(1).TryGetLastSyncTime(Arg.Is<string>(str => str == _dataStreamId), out Arg.Any<long>());
            Assert.Equal(1, _eventHubProducerClient.CreateBatchAsyncCalls);
            Assert.Equal(1, _eventHubProducerClient.SendAsyncCalls);
            _googleFitUser.Received(1).SaveLastSyncTime(Arg.Is<string>(str => str == _dataStreamId), Arg.Is<long>(dto => dto == _lastSyncTime + 1));
        }

        [Theory]
        [InlineData(300, 300, 1, 59, 65)]
        [InlineData(300, 1, 300, 59, 65)]
        [InlineData(300, 30, 10, 59, 65)]
        [InlineData(300, 5, 60, 59, 65)]
        [InlineData(1200, 60, 20, 59, 65)]
        [InlineData(int.MaxValue, 300, 1, 0, 5)]
        [InlineData(int.MaxValue, 1, 300, 0, 5)]
        [InlineData(int.MaxValue, 30, 10, 0, 5)]
        [InlineData(int.MaxValue, 5, 60, 0, 5)]
        [InlineData(int.MaxValue, 60, 20, 0, 5)]
        public async Task GivenMaximumRequestsPerMinuteIsSet_WhenProcessDatasetRequestsIsCalled_RequestsAreThrottledAsExpected(int maxRequestsPerMinute, int dataSourcesCount, int pageCount, int minSeconds, int maxSeconds)
        {
            var context = new GoogleFitDataImporterConfiguration
            {
                MaxRequestsPerMinute = maxRequestsPerMinute,
                MaxConcurrency = 10,
            };

            var options = new GoogleFitImportOptions(context);

            _googleFitImportService = new GoogleFitImportService(
                _googleFitClient,
                azureConfiguration: null,
                _eventHubProducerClient,
                options,
                () => DateTimeOffset.UtcNow,
                _importServiceLogger,
                _telemetryLogger);

            _googleFitUser.TryGetLastSyncTime(Arg.Any<string>(), out Arg.Any<long>())
                .Returns(x =>
                {
                    x[1] = _oneDayBack;
                    return true;
                });

            var dataSources = new List<DataSource>();

            for (int i = 0; i < dataSourcesCount; i++)
            {
                MedTechDataset dataset = Substitute.For<MedTechDataset>(_dataset, _dataSource);
                dataset.GetMaxEndTimeNanos().Returns(_lastSyncTime);

                if (pageCount <= 1)
                {
                    dataset.GetPageToken().Returns((string)null);
                }
                else
                {
                    string nextPageToken = "nextPage";
                    var pageTokenResponses = new List<Func<CallInfo, string>>();

                    for (int j = 1; j < pageCount; j++)
                    {
                        if (j == pageCount - 1)
                        {
                            pageTokenResponses.Add(x => null);
                            continue;
                        }

                        pageTokenResponses.Add(x => nextPageToken);
                    }

                    dataset.GetPageToken().Returns(x => nextPageToken, pageTokenResponses.ToArray());
                }

                DataSource dataSource = new DataSource($"{_dataStreamId}{i}", _deviceUid, _applicationPackageName);
                dataSources.Add(dataSource);

                _googleFitClient.DatasetRequest(
                Arg.Is<string>(access => access == _tokensResponse.AccessToken),
                Arg.Is<DataSource>(d => d == dataSource),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Is<CancellationToken>(token => token == _cancellationToken),
                Arg.Any<string>()).Returns(dataset);
            }

            DateTimeOffset before = DateTimeOffset.Now;

            await _googleFitImportService.ProcessDatasetRequests(_googleFitUser, dataSources, _tokensResponse, _cancellationToken);

            DateTimeOffset after = DateTimeOffset.Now;

            TimeSpan totalProcessTime = after - before;

            Assert.True(totalProcessTime > TimeSpan.FromSeconds(minSeconds), $"Request rate is greater than maximum. Expected {maxRequestsPerMinute} per min actual {dataSourcesCount * pageCount} requests performed in {totalProcessTime.TotalMinutes} mins.");
            Assert.False(totalProcessTime > TimeSpan.FromSeconds(maxSeconds), $"Request rate is out of range. Expected {maxRequestsPerMinute} per min actual {dataSourcesCount * pageCount} requests performed in {totalProcessTime.TotalMinutes} mins.");
        }

        private void SetupMockSuccessReturns()
        {
            string nullPageToken = null;

            _medTechDataset.GetPageToken().Returns(nullPageToken);
            _medTechDataset.GetMaxEndTimeNanos().Returns(_lastSyncTime);

            _googleFitUser.TryGetLastSyncTime(_dataStreamId, out Arg.Any<long>())
                .Returns(x =>
                {
                    x[1] = _oneDayBack;
                    return true;
                });

            _googleFitClient.DatasetRequest(
                Arg.Is<string>(access => access == _tokensResponse.AccessToken),
                Arg.Any<DataSource>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Is<CancellationToken>(token => token == _cancellationToken),
                Arg.Is<string>(str => str == null)).Returns(_medTechDataset);
        }
    }
}
