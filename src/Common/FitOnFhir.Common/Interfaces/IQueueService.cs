// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Microsoft.Health.FitOnFhir.Common.Config;
using Microsoft.Health.FitOnFhir.Common.Models;

namespace Microsoft.Health.FitOnFhir.Common.Interfaces
{
    /// <summary>
    /// An interface for working with Azure storage queues
    /// </summary>
    public interface IQueueService
    {
        /// <summary>
        /// Sends a <see cref="QueueMessage"/> via the Queue identified by the connection string in <see cref="AzureConfiguration"/>.StorageAccountConnectionString
        /// and name contained in <see cref="Constants"/>.QueueName
        /// </summary>
        /// <param name="userId">The ID of the user, which contains the platform specific info for importing.</param>
        /// <param name="platformUserId">The user ID for the platform that the data to import is stored in.</param>
        /// <param name="platformName">The name of the platform that the data to be imported is stored in.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>A Task which contains the <see cref="Response{T}"/> of the send message call.</returns>
        Task SendQueueMessage(string userId, string platformUserId, string platformName, CancellationToken cancellationToken);
    }
}
