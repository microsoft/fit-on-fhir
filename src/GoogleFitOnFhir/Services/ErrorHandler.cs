// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using GoogleFitOnFhir.Models;

namespace GoogleFitOnFhir.Services
{
    public class ErrorHandler : IErrorHandler
    {
        public ErrorHandler()
        {
        }

        /// <inheritdoc/>
        public void HandleDataSyncError(QueueMessage message, Exception exception)
        {
        }
    }
}
