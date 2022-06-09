// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Common.Service;

namespace FitOnFhir.GoogleFit.Client
{
    public class GoogleFitDataImporterContext
    {
        /// <summary>
        /// Sets the results limit of a Dataset request
        /// </summary>
        public int GoogleFitDatasetRequestLimit { get; set; } = 1000;

        /// <summary>
        /// The maximum number of <see cref="ParallelTaskWorker{TOptions}"/> worker threads that are allowed to
        /// be active for this platform, at one time.
        /// </summary>
        public int MaxConcurrency { get; set; } = 10;

        public int MaxRequestsPerMinute { get; set; } = 300;
    }
}
