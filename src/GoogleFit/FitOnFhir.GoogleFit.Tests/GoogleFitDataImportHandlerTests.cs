// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Common.Handler;
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
        private readonly ILogger<GoogleFitDataImportHandler> _logger;

        private static readonly string _fakePlatform = "ACME";
        private static readonly string _fakePlatformUser = "platform user";
        private static readonly string _testUser = "tester";

        public GoogleFitDataImportHandlerTests()
        {
            _googleFitDataImporter = Substitute.For<IGoogleFitDataImporter>();
            _logger = NullLogger<GoogleFitDataImportHandler>.Instance;

            _googleFitDataImportHandler = new GoogleFitDataImportHandler(_googleFitDataImporter, _logger);
        }

        [Fact]
        public void GivenImportRequestForInvalidPlatform_WhenEvaluateIsCalled_NullIsReturned()
        {
            var importRequest = CreateImportRequest(_testUser, _fakePlatformUser, _fakePlatform);
            var result = _googleFitDataImportHandler.Evaluate(importRequest);

            Assert.Null(result);
        }

        [Fact]
        public async Task GivenImportRequestForGoogleFit_WhenEvaluateCalled_TaskCompletes()
        {
            var importRequest = CreateImportRequest(_testUser, _fakePlatformUser, GoogleFitConstants.GoogleFitPlatformName);
            var result = await _googleFitDataImportHandler.Evaluate(importRequest);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GivenImportRequestForGoogleFitThrowsException_WhenEvaluateIsCalled_ExceptionIsThrown()
        {
            string exceptionMessage = "data sync error";
            _googleFitDataImporter.Import(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new Exception(exceptionMessage));

            var importRequest = CreateImportRequest(_testUser, _fakePlatformUser, GoogleFitConstants.GoogleFitPlatformName);
            await Assert.ThrowsAsync<Exception>(() => _googleFitDataImportHandler.Evaluate(importRequest));
        }

        private ImportRequest CreateImportRequest(string user, string platformUser, string platformName)
        {
            return new ImportRequest(new QueueMessage(user, platformUser, platformName), CancellationToken.None);
        }
    }
}
