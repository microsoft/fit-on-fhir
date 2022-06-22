// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Azure.Storage.Sas;
using FitOnFhir.Common.Interfaces;
using FitOnFhir.Common.Services;
using FitOnFhir.Common.Tests.Mocks;
using FitOnFhir.GoogleFit.Common;
using FitOnFhir.GoogleFit.Tests.Mocks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using Xunit;
using QueueMessage = FitOnFhir.Common.Models.QueueMessage;

namespace FitOnFhir.GoogleFit.Tests
{
    public class QueueServiceTests
    {
        private QueueClient _queueClient;
        private MockLogger<QueueService> _queueServiceLogger;
        private IQueueService _queueService;
        private readonly string _expectedUserId;

        public QueueServiceTests()
        {
            _expectedUserId = Guid.NewGuid().ToString();
            _queueClient = Substitute.For<QueueClient>();
            _queueServiceLogger = Substitute.For<MockLogger<QueueService>>();

            _queueService = new QueueService(null, _queueClient, _queueServiceLogger);
        }

        protected string ExpectedUserId => _expectedUserId;

        protected string ExpectedPlatformUserId => "me";

        protected string ExpectedPlatformName => GoogleFitConstants.GoogleFitPlatformName;

        protected string ExpectedMessageText => JsonConvert.SerializeObject(new QueueMessage(ExpectedUserId, ExpectedPlatformUserId, ExpectedPlatformName));

        [Fact]
        public async Task GivenQueueExists_WhenInitQueueCalled_NoQueueIsCreated()
        {
            string loggerMsg = "Queue import-data created";
            _queueClient.CreateIfNotExistsAsync().ReturnsNull();

            await _queueService.SendQueueMessage(ExpectedUserId, ExpectedPlatformUserId, ExpectedPlatformName, CancellationToken.None);

            _queueServiceLogger.DidNotReceive().Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Information),
                Arg.Is<string>(msg => msg == loggerMsg));

            await _queueClient.Received(1).CreateIfNotExistsAsync();
        }

        [Fact]
        public async Task GivenQueueDoesNotExist_WhenInitQueueCalled_QueueIsCreated()
        {
            string loggerMsg = "Queue import-data created";
            _queueClient.CreateIfNotExistsAsync().Returns(Substitute.For<Response>());

            await _queueService.SendQueueMessage(ExpectedUserId, ExpectedPlatformUserId, ExpectedPlatformName, CancellationToken.None);

            _queueServiceLogger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Information),
                Arg.Is<string>(msg => msg == loggerMsg));

            await _queueClient.Received(1).CreateIfNotExistsAsync();
        }

        [Fact]
        public void GivenRequestFailedExceptionIsThrown_WhenInitQueueCalled_ExceptionIsCaughtAndLogged()
        {
            string exceptionMessage = "CreateIfNotExistsAsync exception";
            var exception = new RequestFailedException(exceptionMessage);

            _queueClient.CreateIfNotExistsAsync().Throws(exception);

            Assert.ThrowsAsync<RequestFailedException>(async () => await _queueService.SendQueueMessage(ExpectedUserId, ExpectedPlatformUserId, ExpectedPlatformName, CancellationToken.None));

            _queueServiceLogger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                Arg.Any<RequestFailedException>(),
                Arg.Is<string>(msg => msg == exceptionMessage));
        }

        [Fact]
        public async Task GivenNoConditions_WhenSendQueueMessageCalled_SendMessageAsyncIsCalled()
        {
            _queueClient.SendMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Substitute.For<Response<SendReceipt>>());
            _queueClient.CreateIfNotExistsAsync().Returns(Substitute.For<Response>());

            await _queueService.SendQueueMessage(ExpectedUserId, ExpectedPlatformUserId, ExpectedPlatformName, CancellationToken.None);

            await _queueClient.Received(1).SendMessageAsync(Arg.Is<string>(str => str == ExpectedMessageText), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void GivenExceptionIsThrown_WhenSendQueueMessageCalled_ExceptionIsCaughtAndLogged()
        {
            string exceptionMessage = "SendMessageAsync exception";
            var exception = new Exception(exceptionMessage);

            _queueClient.SendMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(exception);
            _queueClient.CreateIfNotExistsAsync().Returns(Substitute.For<Response>());

            Assert.ThrowsAsync<Exception>(async () => await _queueService.SendQueueMessage(ExpectedUserId, ExpectedPlatformUserId, ExpectedPlatformName, CancellationToken.None));

            _queueServiceLogger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                Arg.Any<Exception>(),
                Arg.Is<string>(msg => msg == exceptionMessage));
        }
    }
}
