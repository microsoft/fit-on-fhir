// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Logging.Telemetry;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Client.Telemetry
{
    public class GoogleFitExceptionTelemetryProcessor : ExceptionTelemetryProcessor
    {
        public override bool HandleException(Exception ex, ITelemetryLogger logger)
        {
            EnsureArg.IsNotNull(ex, nameof(ex));
            EnsureArg.IsNotNull(logger, nameof(logger));

            var exceptionTypeName = ex.GetType().Name;
            var handledExceptionMetric = ex is NotSupportedException ? GoogleFitMetrics.NotSupported() : GoogleFitMetrics.HandledException(exceptionTypeName, GoogleFitMetrics.ImportOperation);
            return HandleException(ex, logger, handledExceptionMetric);
        }
    }
}
