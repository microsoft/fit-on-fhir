// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.FitOnFhir.Common.Handlers;
using Microsoft.Health.FitOnFhir.Common.Requests;
using Microsoft.Health.FitOnFhir.GoogleFit.Common;
using Microsoft.Health.FitOnFhir.GoogleFit.Services;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Client.Handlers
{
    public class GoogleFitDataImportHandler : RequestHandlerBase<ImportRequest, Task<bool?>>
    {
        private readonly IGoogleFitDataImporter _googleFitDataImporter;
        private readonly ILogger<GoogleFitDataImportHandler> _logger;

        private GoogleFitDataImportHandler()
        {
        }

        public GoogleFitDataImportHandler(
            IGoogleFitDataImporter googleFitDataImporter,
            ILogger<GoogleFitDataImportHandler> logger)
        {
            _googleFitDataImporter = EnsureArg.IsNotNull(googleFitDataImporter);
            _logger = EnsureArg.IsNotNull(logger);
        }

        public override IEnumerable<string> HandledRoutes => new List<string>()
        {
            GoogleFitConstants.GoogleFitPlatformName,
        };

        public override async Task<bool?> EvaluateRequest(ImportRequest request)
        {
            await _googleFitDataImporter.Import(request.Message.UserId, request.Message.PlatformUserId, request.Token);
            return true;
        }
    }
}
