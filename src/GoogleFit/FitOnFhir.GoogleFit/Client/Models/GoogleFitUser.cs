// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Azure.Data.Tables;
using FitOnFhir.GoogleFit.Common;

namespace FitOnFhir.GoogleFit.Client.Models
{
    public class GoogleFitUser : ITableEntity
    {
        public GoogleFitUser(string userId)
        {
            PartitionKey = GoogleFitConstants.GoogleFitPlatformName;
            RowKey = userId;
            SyncTimes = new Dictionary<string, DateTimeOffset>();
        }

        public GoogleFitUser()
        {
        }

        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }

        public Dictionary<string, DateTimeOffset> SyncTimes { get; set; }
    }
}
