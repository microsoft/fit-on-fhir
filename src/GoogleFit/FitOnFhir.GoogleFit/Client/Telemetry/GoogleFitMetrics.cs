// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Common.Telemetry;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Client.Telemetry
{
    public static class GoogleFitMetrics
    {
        private static Metric _notSupported = nameof(NotSupportedException).ToErrorMetric(ImportOperation, GoogleFitError, ErrorSeverity.Warning);

        public static string ImportOperation => "GoogleFitImportOperation";

        public static string GoogleFitError => "GoogleFitError";

        /// <summary>
        /// A metric for when FHIR resource does not support the provided type as a value.
        /// </summary>
        public static Metric NotSupported()
        {
            return _notSupported;
        }

        public static Metric UnhandledException(string exceptionName, string operation)
        {
            EnsureArg.IsNotNullOrWhiteSpace(exceptionName);

            return nameof(UnhandledException).ToErrorMetric(operation, ErrorType.GeneralError, ErrorSeverity.Critical, errorName: exceptionName);
        }

        public static Metric HandledException(string exceptionName, string operation)
        {
            return exceptionName.ToErrorMetric(operation, ErrorType.GeneralError, ErrorSeverity.Critical);
        }
    }
}
