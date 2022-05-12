// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using GoogleFitOnFhir.Clients.GoogleFit.Telemetry;
using Microsoft.Health.Common.Config;
using Microsoft.Health.Logging.Telemetry;

namespace GoogleFitOnFhir.Clients.GoogleFit.Config
{
    public class GoogleFitImportOptions
    {
        public virtual ParallelTaskOptions ParallelTaskOptions { get; } = new ParallelTaskOptions { MaxConcurrency = 10 };

        public virtual IExceptionTelemetryProcessor ExceptionService { get; } = new GoogleFitExceptionTelemetryProcessor();
    }
}
