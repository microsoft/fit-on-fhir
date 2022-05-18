// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using FitOnFhir.Common.Models;

namespace GoogleFitOnFhir.Services
{
    public interface IErrorHandler
    {
        /// <summary>
        /// Method for logging and recovering gracefully from data import errors.
        /// </summary>
        /// <param name="message">The <see cref="QueueMessage"/> which resulted in a data import error.</param>
        /// <param name="exception">The exception thrown when the error occurred.</param>
        void HandleDataImportError(QueueMessage message, Exception exception);
    }
}
