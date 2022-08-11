// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Azure.WebJobs;

namespace Microsoft.Health.FitOnFhir.Common.Services
{
    public interface IImportTriggerMessageService
    {
        /// <summary>
        /// Calculates the import trigger messages that should be added to the collector and adds them.
        /// </summary>
        /// <param name="collector">An instance of <see cref="ICollector{T}"/> that collects import trigger messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/></param>
        /// <returns><see cref="Task"/></returns>
        Task AddImportMessagesToCollector(ICollector<string> collector, CancellationToken cancellationToken);
    }
}
