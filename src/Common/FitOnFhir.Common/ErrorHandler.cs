// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.FitOnFhir.Common.Interfaces;

namespace Microsoft.Health.FitOnFhir.Common
{
    public class ErrorHandler : IErrorHandler
    {
        public ErrorHandler()
        {
        }

        /// <inheritdoc/>
        public void HandleDataImportError(string message, Exception exception)
        {
        }
    }
}
