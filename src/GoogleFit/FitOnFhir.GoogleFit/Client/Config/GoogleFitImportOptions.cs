﻿// -------------------------------------------------------------------------------------------------
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
        public GoogleFitImportOptions()
        {
        }

        public GoogleFitImportOptions(GoogleFitDataImporterConfiguration config)
        {
            ParallelTaskOptions = new ParallelTaskOptions { MaxConcurrency = config.MaxConcurrency };
            DataPointPageLimit = config.DatasetRequestLimit;
            MaximumRequestsPerMinute = config.MaxConcurrency;
        }

        public virtual ParallelTaskOptions ParallelTaskOptions { get; }

        public virtual IExceptionTelemetryProcessor ExceptionService { get; } = new GoogleFitExceptionTelemetryProcessor();

        public virtual int DataPointPageLimit { get; }

        public virtual int MaximumRequestsPerMinute { get; }
    }
}
