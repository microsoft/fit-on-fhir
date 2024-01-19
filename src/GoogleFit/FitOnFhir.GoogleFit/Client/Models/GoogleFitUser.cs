// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using Azure.Data.Tables;
using EnsureThat;
using Microsoft.Health.FitOnFhir.Common.Models;
using Microsoft.Health.FitOnFhir.GoogleFit.Common;
using Newtonsoft.Json;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Client.Models
{
    public class GoogleFitUser : EntityBase
    {
        private const string _lastSyncTimesKey = "LastSyncTimes";
        private readonly ConcurrentDictionary<string, long> _lastSyncTimes = new ConcurrentDictionary<string, long>();

        public GoogleFitUser()
            : base(new TableEntity())
        {
        }

        public GoogleFitUser(string userId)
            : this(new TableEntity(GoogleFitConstants.GoogleFitPartitionKey, userId))
        {
        }

        public GoogleFitUser(TableEntity tableEntity)
            : base(tableEntity)
        {
            string serializedLastSyncTimes = InternalTableEntity.GetString(_lastSyncTimesKey);

            if (serializedLastSyncTimes != null)
            {
                _lastSyncTimes = JsonConvert.DeserializeObject<ConcurrentDictionary<string, long>>(serializedLastSyncTimes);
            }
        }

        /// <summary>
        /// Retrieves all of the stored key value pairs of DataSource sync times for this user
        /// </summary>
        /// <returns>The stored sync times in array format.</returns>
        public KeyValuePair<string, long>[] GetLastSyncTimes()
        {
            return _lastSyncTimes.ToArray();
        }

        /// <summary>
        /// Retrieves the last time the DataSource was synced for this user, based on the
        /// data stream ID provided
        /// </summary>
        /// <param name="dataStreamId">The data stream ID for the DataSource.</param>
        /// <param name="lastSyncTimeNanos">The last time a sync was executed for the data stream in nanosecond epoch time format.</param>
        /// <returns><see cref="bool"/> true if the lastSyncTimeNanos exists.</returns>
        public virtual bool TryGetLastSyncTime(string dataStreamId, out long lastSyncTimeNanos)
        {
            EnsureArg.IsNotNullOrWhiteSpace(dataStreamId, nameof(dataStreamId));

            if (_lastSyncTimes.TryGetValue(dataStreamId, out long syncTimeNanos))
            {
                lastSyncTimeNanos = syncTimeNanos;
                return true;
            }

            lastSyncTimeNanos = default;
            return false;
        }

        /// <summary>
        /// Stores the last time a DataSource was synced for this user, as identified
        /// by the data stream ID.
        /// </summary>
        /// <param name="dataStreamId">The data stream ID for the DataSource.</param>
        /// <param name="time">The <see cref="long"/> representing when this DataSource was last synced in nanosecond epoch time format.</param>
        public virtual void SaveLastSyncTime(string dataStreamId, long time)
        {
            _lastSyncTimes.AddOrUpdate(dataStreamId, time, (key, oldTime) => time > oldTime ? time : oldTime);
        }

        public override TableEntity ToTableEntity()
        {
            if (_lastSyncTimes != null && !_lastSyncTimes.IsEmpty)
            {
                string serializedLastSyncTimes = JsonConvert.SerializeObject(_lastSyncTimes);
                InternalTableEntity.Add(_lastSyncTimesKey, serializedLastSyncTimes);
            }

            return base.ToTableEntity();
        }
    }
}
