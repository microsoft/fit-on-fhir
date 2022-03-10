// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Azure;
using Azure.Data.Tables;

namespace GoogleFitOnFhir.Models
{
    public class User : ITableEntity
    {
        public User(string userId)
        {
            PartitionKey = userId;
            RowKey = userId;
            Id = userId;
        }

        public User()
        {
        }

        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }

        public string Id { get; set; }

        public DateTimeOffset? LastSync { get; set; }
    }
}
