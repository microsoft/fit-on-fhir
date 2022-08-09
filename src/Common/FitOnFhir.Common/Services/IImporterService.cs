// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Storage.Queues.Models;
using Microsoft.Health.Common.Handler;

namespace Microsoft.Health.FitOnFhir.Common.Services
{
    public interface IImporterService
    {
        /// <summary>
        /// Passes along a string to an <see cref="IResponsibilityHandler{TRequest,TResult}"/>, which
        /// can then evaluate it and have the appropriate platform specific data importing handler take action.
        /// </summary>
        /// <param name="message">A <see cref="string"/> to take action on (should deserialize to a <see cref="QueueMessage"/> object.</param>
        /// <param name="cancellationToken">A cancellation token for graceful recovery.</param>
        public Task Import(string message, CancellationToken cancellationToken);
    }
}
