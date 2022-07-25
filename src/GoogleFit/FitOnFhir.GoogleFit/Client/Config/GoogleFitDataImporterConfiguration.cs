// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Common.Service;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Client.Config
{
    public class GoogleFitDataImporterConfiguration
    {
        /// <summary>
        /// Sets the results limit of a Dataset request
        /// </summary>
        public int DatasetRequestLimit { get; set; }

        /// <summary>
        /// The maximum number of <see cref="ParallelTaskWorker{TOptions}"/> worker threads that are allowed to
        /// be active for this platform, at one time.
        /// </summary>
        public int MaxConcurrency { get; set; }

        /// <summary>
        /// The maximum number of requests that can he handled per minute by the Google APIs.
        /// </summary>
        public int MaxRequestsPerMinute { get; set; }

        /// <summary>
        /// The period of time from now into the past, that the first Google Fit data import should cover.
        /// </summary>
        public TimeSpan HistoricalImportTimeSpan { get; set; }
    }
}
