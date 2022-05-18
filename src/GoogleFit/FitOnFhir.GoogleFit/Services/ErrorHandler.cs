// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using FitOnFhir.Common.Models;

namespace FitOnFhir.GoogleFit.Services
{
    public class ErrorHandler : IErrorHandler
    {
        public ErrorHandler()
        {
        }

        /// <inheritdoc/>
        public void HandleDataImportError(QueueMessage message, Exception exception)
        {
        }
    }
}
