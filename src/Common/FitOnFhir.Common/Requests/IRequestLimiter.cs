// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.FitOnFhir.Common.Requests
{
    public interface IRequestLimiter
    {
        /// <summary>
        /// Will calculate a required delay to maintain a specific request rate.
        /// </summary>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <param name="delayTask">A task that will delay the appropriate amount of time to maintain the request rate.</param>
        /// <param name="delayMs">The amount of delay in milliseconds recommended.</param>
        /// <returns><see cref="bool"/>true if throttling is required.</returns>
        bool TryThrottle(CancellationToken cancellationToken, out Task delayTask, out double delayMs);
    }
}
