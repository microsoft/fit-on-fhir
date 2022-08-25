// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.FitOnFhir.Common.Models;

namespace Microsoft.Health.FitOnFhir.Common.Interfaces
{
    public interface IErrorHandler
    {
        /// <summary>
        /// Method for logging and recovering gracefully from data import errors.
        /// </summary>
        /// <param name="message">The serialized <see cref="QueueMessage"/> which resulted in a data import error.</param>
        /// <param name="exception">The exception thrown when the error occurred.</param>
        void HandleDataImportError(string message, Exception exception);
    }
}
