// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.GoogleFit.Client.Telemetry;
using Microsoft.Health.Common.Config;
using Microsoft.Health.Logging.Telemetry;

namespace FitOnFhir.GoogleFit.Client.Config
{
    public class GoogleFitImportOptions
    {
        public virtual ParallelTaskOptions ParallelTaskOptions { get; } = new ParallelTaskOptions { MaxConcurrency = GoogleFitDataImporterContext.MaxConcurrency };

        public virtual IExceptionTelemetryProcessor ExceptionService { get; } = new GoogleFitExceptionTelemetryProcessor();
    }
}
