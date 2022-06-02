// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using Azure.Data.Tables;
using EnsureThat;
using FitOnFhir.Common.Models;
using FitOnFhir.GoogleFit.Common;
using Newtonsoft.Json;

namespace FitOnFhir.GoogleFit.Client.Models
{
    public class GoogleFitUser : EntityBase
    {
        private const string _lastSyncTimesKey = "LastSyncTimes";
        private readonly ConcurrentDictionary<string, DateTimeOffset> _lastSyncTimes = new ConcurrentDictionary<string, DateTimeOffset>();

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
                _lastSyncTimes = JsonConvert.DeserializeObject<ConcurrentDictionary<string, DateTimeOffset>>(serializedLastSyncTimes);
            }
        }

        /// <summary>
        /// Retrieves the last time the DataSource was synced for this user, based on the
        /// data stream ID provided
        /// </summary>
        /// <param name="dataStreamId">The data stream ID for the DataSource.</param>
        /// <param name="lastSyncTime">The last time a sync was executed for the data stream.</param>
        /// <returns>The <see cref="DateTimeOffset"/> for the last sync.</returns>
        public virtual bool TryGetLastSyncTime(string dataStreamId, out DateTimeOffset lastSyncTime)
        {
            EnsureArg.IsNotNullOrWhiteSpace(dataStreamId, nameof(dataStreamId));

            if (_lastSyncTimes.TryGetValue(dataStreamId, out DateTimeOffset syncTime))
            {
                lastSyncTime = syncTime;
                return true;
            }

            lastSyncTime = default;
            return false;
        }

        /// <summary>
        /// Stores the last time a DataSource was synced for this user, as identified
        /// by the data stream ID.
        /// </summary>
        /// <param name="dataStreamId">The data stream ID for the DataSource.</param>
        /// <param name="time">The <see cref="DateTimeOffset"/> representing when this DataSource was last synced.</param>
        public virtual void SaveLastSyncTime(string dataStreamId, DateTimeOffset time)
        {
            _lastSyncTimes.AddOrUpdate(dataStreamId, time, (key, oldTime) => time > oldTime ? time : oldTime);
        }

        public override TableEntity ToTableEntity()
        {
            if (_lastSyncTimes != null && _lastSyncTimes.Count > 0)
            {
                string serializedLastSyncTimes = JsonConvert.SerializeObject(_lastSyncTimes);
                InternalTableEntity.Add(_lastSyncTimesKey, serializedLastSyncTimes);
            }

            return base.ToTableEntity();
        }
    }
}
