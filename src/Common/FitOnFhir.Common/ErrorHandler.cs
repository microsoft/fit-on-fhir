// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.FitOnFhir.Common.Interfaces;
using Microsoft.Health.FitOnFhir.Common.Models;

namespace Microsoft.Health.FitOnFhir.Common
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
