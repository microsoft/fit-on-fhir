// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.Common.Interfaces;
using FitOnFhir.Common.Models;
using FitOnFhir.Common.Requests;
using FitOnFhir.GoogleFit.Client.Handlers;
using FitOnFhir.GoogleFit.Common;
using FitOnFhir.GoogleFit.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Common.Handler;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace FitOnFhir.GoogleFit.Tests
{
    public class GoogleFitDataImportHandlerTests
    {
        private readonly IResponsibilityHandler<ImportRequest, Task> _googleFitPublishingHandler;
        private readonly IGoogleFitDataImporter _googleFitDataImporter;
        private readonly IErrorHandler _errorHandler;
        private readonly ILogger<GoogleFitDataImportHandler> _logger;

        private static string _fakePlatform = "ACME";
        private static string _testUser = "tester";

        public GoogleFitDataImportHandlerTests()
        {
            _googleFitDataImporter = Substitute.For<IGoogleFitDataImporter>();
            _errorHandler = Substitute.For<IErrorHandler>();
            _logger = NullLogger<GoogleFitDataImportHandler>.Instance;

            _googleFitPublishingHandler = new GoogleFitDataImportHandler(_googleFitDataImporter, _errorHandler, _logger);
        }

        [Fact]
        public void GivenPublishRequestForInvalidPlatform_WhenPublishToIsCalled_NullIsReturned()
        {
            var publishRequest = CreatePublishRequest(_testUser, _fakePlatform);
            var result = _googleFitPublishingHandler.Evaluate(publishRequest);

            Assert.Null(result);
        }

        [Fact]
        public void GivenPublishRequestForGoogleFit_WhenPublishToIsCalled_TaskCompletes()
        {
            var publishRequest = CreatePublishRequest(_testUser, GoogleFitConstants.GoogleFitPlatformName);
            var result = _googleFitPublishingHandler.Evaluate(publishRequest);

            Assert.NotNull(result);
        }

        [Fact]
        public void GivenPublishRequestForGoogleFitThrowsException_WhenPublishToIsCalled_HandleDataSyncErrorIsCalled()
        {
            string exceptionMessage = "data sync error";
            _googleFitDataImporter.Import(Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new Exception(exceptionMessage));

            var publishRequest = CreatePublishRequest(_testUser, GoogleFitConstants.GoogleFitPlatformName);
            var result = _googleFitPublishingHandler.Evaluate(publishRequest);

            var expectedQueueMessage = new QueueMessage(_testUser, GoogleFitConstants.GoogleFitPlatformName);
            _errorHandler.Received(1).HandleDataImportError(
                Arg.Is<QueueMessage>(msg => msg.UserId == expectedQueueMessage.UserId && msg.PlatformName == expectedQueueMessage.PlatformName),
                Arg.Is<Exception>(ex => ex.Message == exceptionMessage));
            Assert.Null(result);
        }

        private ImportRequest CreatePublishRequest(string user, string platform)
        {
            return new ImportRequest(new QueueMessage(user, platform), CancellationToken.None);
        }
    }
}
