// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace FitOnFhir.Common.Requests
{
    public class RequestLimiter : IRequestLimiter
    {
        private readonly int _maxRequestsPerMinute;
        private readonly Func<DateTimeOffset> _utcNowFunc;
        private readonly object lockObject = new object();

        public RequestLimiter(int maxRequestsPerMinute, Func<DateTimeOffset> utcNowFunc)
        {
            _maxRequestsPerMinute = maxRequestsPerMinute;
            _utcNowFunc = utcNowFunc;
        }

        private DateTimeOffset ProcessStartTime { get; set; }

        private DateTimeOffset NextAvailableRequestStartTime { get; set; }

        private double RequestCount { get; set; }

        public bool TryThrottle(CancellationToken cancellationToken, out Task delayTask, out double delayMs)
        {
            lock (lockObject)
            {
                try
                {
                    if (RequestCount == 0)
                    {
                        // First request, set the request time to now, so it is sent immediately.
                        var now = _utcNowFunc();
                        ProcessStartTime = now;
                        NextAvailableRequestStartTime = now;
                    }
                    else
                    {
                        // Calculate the required delay to remain under the requests per minute limit.
                        double averageRequiredRequestDurationInSeconds = 120 / (float)_maxRequestsPerMinute;
                        double currentRequestDurationInSeconds = 1 / (RequestCount / (_utcNowFunc() - ProcessStartTime).TotalSeconds);
                        double delayInSeconds = averageRequiredRequestDurationInSeconds - currentRequestDurationInSeconds;

                        if (delayInSeconds > 0)
                        {
                            // Calculate the new next available start time.
                            NextAvailableRequestStartTime = NextAvailableRequestStartTime.AddSeconds(delayInSeconds);
                        }
                    }

                    TimeSpan delayTimeSpan = NextAvailableRequestStartTime - _utcNowFunc();

                    if (delayTimeSpan > TimeSpan.Zero)
                    {
                        delayTask = Task.Delay(delayTimeSpan, cancellationToken);
                        delayMs = delayTimeSpan.TotalMilliseconds;
                        return true;
                    }

                    delayTask = Task.CompletedTask;
                    delayMs = 0;
                    return false;
                }
                finally
                {
                    RequestCount++;
                }
            }
        }
    }
}
