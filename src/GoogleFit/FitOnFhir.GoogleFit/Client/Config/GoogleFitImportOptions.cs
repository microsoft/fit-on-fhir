// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Common.Config;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Telemetry;
using Microsoft.Health.Logging.Telemetry;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Client.Config
{
    public class GoogleFitImportOptions
    {
        public GoogleFitImportOptions()
        {
        }

        public GoogleFitImportOptions(GoogleFitDataImporterConfiguration config)
        {
            EnsureArg.IsNotNull(config, nameof(config));

            ParallelTaskOptions = new ParallelTaskOptions { MaxConcurrency = config.MaxConcurrency };
            DataPointPageLimit = config.DatasetRequestLimit;
            MaxConcurrency = config.MaxConcurrency;
            MaxRequestsPerMinute = config.MaxRequestsPerMinute;
            HistoricalImportTimeSpan = config.HistoricalImportTimeSpan;
        }

        public virtual ParallelTaskOptions ParallelTaskOptions { get; }

        public virtual IExceptionTelemetryProcessor ExceptionService { get; } = new GoogleFitExceptionTelemetryProcessor();

        public virtual int DataPointPageLimit { get; }

        public virtual int MaxConcurrency { get; }

        public virtual int MaxRequestsPerMinute { get; }

        public virtual TimeSpan HistoricalImportTimeSpan { get; }
    }
}
