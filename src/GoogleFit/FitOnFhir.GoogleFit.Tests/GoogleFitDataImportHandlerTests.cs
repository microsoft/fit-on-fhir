// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Common.Handler;
using Microsoft.Health.FitOnFhir.Common.Interfaces;
using Microsoft.Health.FitOnFhir.Common.Models;
using Microsoft.Health.FitOnFhir.Common.Requests;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Handlers;
using Microsoft.Health.FitOnFhir.GoogleFit.Common;
using Microsoft.Health.FitOnFhir.GoogleFit.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Tests
{
    public class GoogleFitDataImportHandlerTests
    {
        private readonly IResponsibilityHandler<ImportRequest, Task<bool?>> _googleFitDataImportHandler;
        private readonly IGoogleFitDataImporter _googleFitDataImporter;
        private readonly IErrorHandler _errorHandler;
        private readonly ILogger<GoogleFitDataImportHandler> _logger;

        private static readonly string _fakePlatform = "ACME";
        private static readonly string _fakePlatformUser = "platform user";
        private static readonly string _testUser = "tester";

        public GoogleFitDataImportHandlerTests()
        {
            _googleFitDataImporter = Substitute.For<IGoogleFitDataImporter>();
            _errorHandler = Substitute.For<IErrorHandler>();
            _logger = NullLogger<GoogleFitDataImportHandler>.Instance;

            _googleFitDataImportHandler = new GoogleFitDataImportHandler(_googleFitDataImporter, _errorHandler, _logger);
        }

        [Fact]
        public void GivenPublishRequestForInvalidPlatform_WhenPublishToIsCalled_NullIsReturned()
        {
            var importRequest = CreateImportRequest(_testUser, _fakePlatformUser, _fakePlatform);
            var result = _googleFitDataImportHandler.Evaluate(importRequest);

            Assert.Null(result);
        }

        [Fact]
        public async Task GivenPublishRequestForGoogleFit_WhenPublishToIsCalled_TaskCompletes()
        {
            var importRequest = CreateImportRequest(_testUser, _fakePlatformUser, GoogleFitConstants.GoogleFitPlatformName);
            var result = await _googleFitDataImportHandler.Evaluate(importRequest);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GivenPublishRequestForGoogleFitThrowsException_WhenPublishToIsCalled_HandleDataSyncErrorIsCalled()
        {
            string exceptionMessage = "data sync error";
            _googleFitDataImporter.Import(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new Exception(exceptionMessage));

            var importRequest = CreateImportRequest(_testUser, _fakePlatformUser, GoogleFitConstants.GoogleFitPlatformName);
            var result = await _googleFitDataImportHandler.Evaluate(importRequest);

            var expectedQueueMessage = new QueueMessage(_testUser, _fakePlatformUser, GoogleFitConstants.GoogleFitPlatformName);
            _errorHandler.Received(1).HandleDataImportError(
                Arg.Is<QueueMessage>(msg => msg.UserId == expectedQueueMessage.UserId && msg.PlatformName == expectedQueueMessage.PlatformName),
                Arg.Is<Exception>(ex => ex.Message == exceptionMessage));
            Assert.Null(result);
        }

        private ImportRequest CreateImportRequest(string user, string platformUser, string platformName)
        {
            return new ImportRequest(new QueueMessage(user, platformUser, platformName), CancellationToken.None);
        }
    }
}
