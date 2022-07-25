// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.FitOnFhir.Common.Requests
{
    public class RequestLimiter : IRequestLimiter
    {
        private const int SecondsPerMinute = 60;
        private const double DelayMultiplier = 2; // Default value is 2. This will keep the request rate at desired maxRequestsPerMinute.
        private readonly int _maxRequestsPerMinute;
        private readonly Func<DateTimeOffset> _utcNowFunc;
        private readonly object lockObject = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestLimiter"/> class.
        /// </summary>
        /// <param name="maxRequestsPerMinute">The target allowed maximum requests per minute.</param>
        /// <param name="utcNowFunc">A function that provides <see cref="DateTimeOffset"/> for now.</param>
        public RequestLimiter(int maxRequestsPerMinute, Func<DateTimeOffset> utcNowFunc)
        {
            // If maxRequestsPerMinute is zero or a negative number, assume no throttling is required.
            _maxRequestsPerMinute = maxRequestsPerMinute > 0 ? maxRequestsPerMinute : int.MaxValue;
            _utcNowFunc = EnsureArg.IsNotNull(utcNowFunc);
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
                        double averageRequiredRequestDurationInSeconds = SecondsPerMinute / (float)_maxRequestsPerMinute;
                        double currentRequestDurationInSeconds = (_utcNowFunc() - ProcessStartTime).TotalSeconds / RequestCount;
                        double delayInSeconds = (averageRequiredRequestDurationInSeconds * DelayMultiplier) - currentRequestDurationInSeconds;

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
