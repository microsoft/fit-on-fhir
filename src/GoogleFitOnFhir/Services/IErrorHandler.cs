// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using GoogleFitOnFhir.Models;

namespace GoogleFitOnFhir.Services
{
    public interface IErrorHandler
    {
        /// <summary>
        /// Method for logging and recovering gracefully from publish data sync errors.
        /// </summary>
        /// <param name="message">The <see cref="QueueMessage"/> which resulted in a data sync error.</param>
        /// <param name="exception">The exception thrown when the error occurred.</param>
        void HandleDataImportError(QueueMessage message, Exception exception);
    }
}
