// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Health.Logging.Telemetry;

namespace GoogleFitOnFhir.Clients.GoogleFit.Telemetry
{
    public class GoogleFitExceptionTelemetryProcessor : ExceptionTelemetryProcessor
    {
        public GoogleFitExceptionTelemetryProcessor()
            : base(typeof(AggregateException))
        {
        }

        public override bool HandleException(Exception ex, ITelemetryLogger logger)
        {
            EnsureArg.IsNotNull(ex, nameof(ex));
            EnsureArg.IsNotNull(logger, nameof(logger));

            return base.HandleException(ex, logger);
        }
    }
}
