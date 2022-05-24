// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using FitOnFhir.Common.Interfaces;
using FitOnFhir.Common.Requests;
using FitOnFhir.GoogleFit.Common;
using FitOnFhir.GoogleFit.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Common.Handler;

namespace FitOnFhir.GoogleFit.Client.Handlers
{
    public class GoogleFitDataImportHandler : IResponsibilityHandler<ImportRequest, Task>
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

        public static IResponsibilityHandler<ImportRequest, Task> Instance { get; } = new GoogleFitDataImportHandler();

        public Task Evaluate(ImportRequest request)
        {
            try
            {
                if (request.Message.PlatformName == GoogleFitConstants.GoogleFitPlatformName)
                {
                    _googleFitDataImporter.Import(request.Message.UserId, request.Token);
                    return Task.CompletedTask;
                }
                else
                {
                    return null;
                }
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
