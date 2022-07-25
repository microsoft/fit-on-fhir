// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.ApplicationInsights;
using Microsoft.Health.Logging.Telemetry;
using Metric = Microsoft.Health.Common.Telemetry.Metric;

namespace Microsoft.Health.FitOnFhir.Common
{
    public class TelemetryLogger : ITelemetryLogger
    {
        private readonly TelemetryClient _telemetryClient;

        public TelemetryLogger(TelemetryClient telemetryClient)
        {
            _telemetryClient = EnsureArg.IsNotNull(telemetryClient, nameof(telemetryClient));
        }

        public virtual void LogMetric(Metric metric, double metricValue)
        {
            EnsureArg.IsNotNull(metric);
            LogMetricWithDimensions(metric, metricValue);
        }

        public void LogError(Exception ex)
        {
            if (ex is AggregateException e)
            {
                // Address bug https://github.com/microsoft/iomt-fhir/pull/120
                LogAggregateException(e);
            }
            else
            {
                LogExceptionWithProperties(ex);
                LogInnerException(ex);
            }
        }

        public void LogTrace(string message)
        {
            _telemetryClient.TrackTrace(message);
        }

        public void LogMetricWithDimensions(Metric metric, double metricValue)
        {
            EnsureArg.IsNotNull(metric);
            _telemetryClient.LogMetric(metric, metricValue);
        }

        private void LogInnerException(Exception ex)
        {
            EnsureArg.IsNotNull(ex, nameof(ex));

            var innerException = ex.InnerException;
            if (innerException != null)
            {
                LogExceptionWithProperties(innerException);
            }
        }

        private void LogAggregateException(AggregateException e)
        {
            LogInnerException(e);

            foreach (var exception in e.InnerExceptions)
            {
                LogExceptionWithProperties(exception);
            }
        }

        private void LogExceptionWithProperties(Exception ex)
        {
            EnsureArg.IsNotNull(ex, nameof(ex));
            _telemetryClient.LogException(ex);
        }
    }
}
