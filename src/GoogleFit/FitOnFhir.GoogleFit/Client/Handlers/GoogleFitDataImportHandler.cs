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

        public GoogleFitDataImportHandler(
            IGoogleFitDataImporter googleFitDataImporter,
            ILogger<GoogleFitDataImportHandler> logger)
        {
            _googleFitDataImporter = EnsureArg.IsNotNull(googleFitDataImporter);
        }

        public override IEnumerable<string> HandledRoutes => new List<string>()
        {
            GoogleFitConstants.GoogleFitPlatformName,
        };

        public override async Task<bool?> EvaluateRequest(ImportRequest request)
        {
            EnsureArg.IsNotNull(request, nameof(request));
            EnsureArg.IsNotNull(request?.Message?.UserId, nameof(request.Message.UserId));
            EnsureArg.IsNotNull(request?.Message?.PlatformUserId, nameof(request.Message.PlatformUserId));

            await _googleFitDataImporter.Import(request.Message.UserId, request.Message.PlatformUserId, request.Token);
            return true;
        }
    }
}
