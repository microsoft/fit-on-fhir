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
        public User(string userId, string platformName)
        {
            PartitionKey = userId;
            RowKey = userId;
            Id = userId;
            PlatformName = platformName;
        }

        public User()
        {
        }

        public string? PartitionKey { get; set; }

        public string? RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }

        public string? Id { get; set; }

        public DateTimeOffset? LastSync { get; set; }

        public string? PlatformName { get; set; }
    }
}
