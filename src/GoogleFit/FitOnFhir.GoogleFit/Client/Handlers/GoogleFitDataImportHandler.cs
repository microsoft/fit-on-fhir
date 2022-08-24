// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.FitOnFhir.Common.Handlers;
using Microsoft.Health.FitOnFhir.Common.Interfaces;
using Microsoft.Health.FitOnFhir.Common.Requests;
using Microsoft.Health.FitOnFhir.GoogleFit.Common;
using Microsoft.Health.FitOnFhir.GoogleFit.Services;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Client.Handlers
{
    public class GoogleFitDataImportHandler : RequestHandlerBase<ImportRequest, Task<bool?>>
    {
        private readonly IGoogleFitDataImporter _googleFitDataImporter;
        private readonly IErrorHandler _errorHandler;
        private readonly ILogger<GoogleFitDataImportHandler> _logger;

        private GoogleFitDataImportHandler()
        {
        }

        public GoogleFitDataImportHandler(
            IGoogleFitDataImporter googleFitDataImporter,
            IErrorHandler errorHandler,
            ILogger<GoogleFitDataImportHandler> logger)
        {
            _googleFitDataImporter = EnsureArg.IsNotNull(googleFitDataImporter);
            _errorHandler = EnsureArg.IsNotNull(errorHandler);
            _logger = EnsureArg.IsNotNull(logger);
        }

        public override IEnumerable<string> HandledRoutes => new List<string>()
        {
            GoogleFitConstants.GoogleFitPlatformName,
        };

        public override async Task<bool?> EvaluateRequest(ImportRequest request)
        {
            try
            {
                await _googleFitDataImporter.Import(request.Message.UserId, request.Message.PlatformUserId, request.Token);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                _errorHandler.HandleDataImportError(request.Message, ex);
                return null;
            }
        }
    }
}
