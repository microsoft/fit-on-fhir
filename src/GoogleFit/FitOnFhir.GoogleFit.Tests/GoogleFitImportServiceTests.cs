﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Messaging.EventHubs;
using FitOnFhir.Common.Exceptions;
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
        private const string _eventBody = "EventBody";
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

        private readonly DateTimeOffset _oneDayBack =
            new DateTimeOffset(2004, 1, 11, 0, 0, 0, new TimeSpan(-5, 0, 0));

        private readonly GoogleFitUser _googleFitUser;
        private readonly MockEventHubProducerClient _eventHubProducerClient;
        private readonly MockFaultyEventHubProducerClient _faultyEventHubProducerClient;
        private readonly EventData _eventData;
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
            _eventData = new EventData(_eventBody);

            _medTechDataset = Substitute.For<MedTechDataset>(_dataset, _dataSource);
            _dataSources.Add(_dataSource);
            _faultyEventHubProducerClient = new MockFaultyEventHubProducerClient();

            var parallelTaskOptions = new ParallelTaskOptions() { MaxConcurrency = 10 };
            _options = Substitute.For<GoogleFitImportOptions>();
            _options.ParallelTaskOptions.Returns(parallelTaskOptions);
            var telemetryLogger = new GoogleFitExceptionTelemetryProcessor();
            _options.ExceptionService.Returns(telemetryLogger);

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

            _googleFitUser.TryGetLastSyncTime(_dataStreamId, out Arg.Any<DateTimeOffset>())
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
            string loggerMsg = "No Dataset for: DataStreamId";

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
            _googleFitUser.DidNotReceive().SaveLastSyncTime(Arg.Any<string>(), Arg.Any<DateTimeOffset>());
        }

        [Fact]
        public async Task GivenEventDataBatchTryAddReturnsFalse_WhenMedTechDatasetAdded_EventBatchExceptionIsLogged()
        {
            string exceptionMessage = "Event is too large for the batch and cannot be sent.";

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
                Arg.Any<EventBatchException>(),
                Arg.Is<string>(msg => msg == exceptionMessage));
        }

        [Fact]
        public void GivenDatasetRequestThrowsException_WhenProcessDatasetRequestsIsCalled_ExceptionIsThrown()
        {
            string exceptionMessage = "DatasetRequest exception";
            var datasetRequestException = new Exception(exceptionMessage);

            _googleFitUser.TryGetLastSyncTime(_dataStreamId, out Arg.Any<DateTimeOffset>())
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
        public async Task GivenDatasetRequestThrowsAggregateException_WhenProcessDatasetRequestsIsCalled_AggregateExceptionIsCaughtByGoogleFitExceptionTelemetryProcessor()
        {
            string exceptionMessage = "DatasetRequest aggregate exception";
            var datasetRequestAggregateException = new AggregateException(exceptionMessage);

            _googleFitUser.TryGetLastSyncTime(_dataStreamId, out Arg.Any<DateTimeOffset>())
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
                Arg.Is<CancellationToken>(token => token == _cancellationToken)).Throws(datasetRequestAggregateException);

            await _googleFitImportService.ProcessDatasetRequests(_googleFitUser, _dataSources, _tokensResponse, _cancellationToken);

            Assert.Equal(0, _eventHubProducerClient.CreateBatchAsyncCalls);
            Assert.Equal(0, _eventHubProducerClient.SendAsyncCalls);
            _googleFitUser.DidNotReceive().SaveLastSyncTime(Arg.Any<string>(), Arg.Any<DateTimeOffset>());
        }

        [Fact]
        public async Task GivenNextPageTokenIsNotNull_WhenProcessDatasetRequestsIsCalled_AllPageResultsAreRetrieved()
        {
            string nextPageToken = "next page";
            string nullPageToken = null;

            _googleFitUser.TryGetLastSyncTime(_dataStreamId, out Arg.Any<DateTimeOffset>())
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
            lastMedTechDataset.GetPageToken().Returns(nullPageToken);

            _googleFitClient.DatasetRequest(
                Arg.Is<string>(access => access == _tokensResponse.AccessToken),
                Arg.Any<DataSource>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Is<CancellationToken>(token => token == _cancellationToken),
                Arg.Is<string>(str => str == nextPageToken)).Returns(lastMedTechDataset);

            await _googleFitImportService.ProcessDatasetRequests(_googleFitUser, _dataSources, _tokensResponse, _cancellationToken);

            _googleFitUser.Received(1).TryGetLastSyncTime(Arg.Is<string>(str => str == _dataStreamId), out Arg.Any<DateTimeOffset>());
            Assert.Equal(2, _eventHubProducerClient.CreateBatchAsyncCalls);
            Assert.Equal(2, _eventHubProducerClient.SendAsyncCalls);
            _googleFitUser.Received(2).SaveLastSyncTime(Arg.Is<string>(str => str == _dataStreamId), Arg.Is<DateTimeOffset>(dto => dto == _utcNowFunc()));
        }

        [Fact]
        public async Task GivenNoConditions_WhenProcessDatasetRequestsIsCalled_Completes()
        {
            SetupMockSuccessReturns();

            await _googleFitImportService.ProcessDatasetRequests(_googleFitUser, _dataSources, _tokensResponse, _cancellationToken);

            _googleFitUser.Received(1).TryGetLastSyncTime(Arg.Is<string>(str => str == _dataStreamId), out Arg.Any<DateTimeOffset>());
            Assert.Equal(1, _eventHubProducerClient.CreateBatchAsyncCalls);
            Assert.Equal(1, _eventHubProducerClient.SendAsyncCalls);
            _googleFitUser.Received(1).SaveLastSyncTime(Arg.Is<string>(str => str == _dataStreamId), Arg.Is<DateTimeOffset>(dto => dto == _utcNowFunc()));
        }

        private void SetupMockSuccessReturns()
        {
            string nullPageToken = null;

            _medTechDataset.GetPageToken().Returns(nullPageToken);

            _googleFitUser.TryGetLastSyncTime(_dataStreamId, out Arg.Any<DateTimeOffset>())
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
