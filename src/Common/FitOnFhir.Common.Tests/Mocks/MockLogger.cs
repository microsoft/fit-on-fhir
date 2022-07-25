// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Microsoft.Health.FitOnFhir.Common.Tests.Mocks
{
    public abstract class MockLogger<T> : ILogger<T>
    {
        public abstract IDisposable BeginScope<TState>(TState state);

        public abstract bool IsEnabled(LogLevel logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (exception == null)
            {
                Log(logLevel, formatter(state, exception));
            }
            else
            {
                Log(logLevel, exception, formatter(state, exception));
            }
        }

        public abstract void Log(LogLevel logLevel, Exception exception, string message);

        public abstract void Log(LogLevel logLevel, string message);
    }
}
