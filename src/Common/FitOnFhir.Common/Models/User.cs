// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Azure.Data.Tables;

namespace FitOnFhir.Common.Models
{
    public class User : ITableEntity
    {
        public User(Guid userId)
        {
            PartitionKey = Constants.UsersPartitionKey;
            RowKey = userId.ToString();
            PlatformUserInfo = new Dictionary<string, string>();
        }

        public User()
        {
        }

        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }

        public DateTimeOffset? LastSync { get; set; }

        /// <summary>
        /// Store any platform names (key) for this user along with their user ID for that platform (value)
        /// </summary>
        public Dictionary<string, string> PlatformUserInfo { get; set; }
    }
}
