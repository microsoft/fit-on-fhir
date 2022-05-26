// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.Common.Models;
using FitOnFhir.GoogleFit.Common;

namespace FitOnFhir.GoogleFit.Client.Models
{
    public class GoogleFitUser : UserBase
    {
        public GoogleFitUser(string userId)
            : base(GoogleFitConstants.GoogleFitPartitionKey, userId)
        {
        }

        public GoogleFitUser()
            : base(string.Empty, string.Empty)
        {
        }

        /// <summary>
        /// Retrieves the last time the DataSource was synced for this user, based on the
        /// data stream ID provided
        /// </summary>
        /// <param name="dataStreamid">The data stream ID for the DataSource.</param>
        /// <returns>The <see cref="DateTimeOffset"/> for the last sync.</returns>
        public DateTimeOffset? GetLastSyncTime(string dataStreamid)
        {
            DateTimeOffset? lastSyncTime = null;

            if (Entity.TryGetValue(dataStreamid, out object time))
            {
                lastSyncTime = time as DateTimeOffset?;
            }

            return lastSyncTime;
        }

        /// <summary>
        /// Stores the last time a DataSource was synced for this user, as identified
        /// by the data stream ID.
        /// </summary>
        /// <param name="dataStreamId">The data stream ID for the DataSource.</param>
        /// <param name="time">The <see cref="DateTimeOffset"/> representing when this DataSource was last synced.</param>
        public void SaveLastSyncTime(string dataStreamId, DateTimeOffset? time)
        {
            Entity[dataStreamId] = time;
        }

        /// <summary>
        /// Converts the underlying Dictionary from having an object value, to a <see cref="DateTimeOffset"/> value.
        /// </summary>
        public Dictionary<string, DateTimeOffset?> ToDictionary()
        {
            return Entity.Select(t => new { t.Key, t.Value })
                .ToDictionary(t => t.Key, t => t.Value as DateTimeOffset?);
        }
    }
}
