// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Common.Service;

namespace FitOnFhir.GoogleFit.Clients.GoogleFit
{
    public class GoogleFitDataImporterContext
    {
        /// <summary>
        /// Sets the results limit of a Dataset request
        /// </summary>
        public static int GoogleFitDatasetRequestLimit => 100;

        /// <summary>
        /// The maximum number of <see cref="ParallelTaskWorker{TOptions}"/> worker threads that are allowed to
        /// be active for this platform, at one time.
        /// </summary>
        public static int MaxConcurrency => 10;
    }
}
