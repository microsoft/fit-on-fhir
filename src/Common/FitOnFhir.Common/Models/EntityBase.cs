// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Azure.Data.Tables;

namespace Microsoft.Health.FitOnFhir.Common.Models
{
    public abstract class EntityBase
    {
        protected EntityBase(TableEntity tableEntity)
        {
            InternalTableEntity = tableEntity;
        }

        protected TableEntity InternalTableEntity { get; }

        public string PartitionKey
        {
            get => InternalTableEntity.PartitionKey;

            set
            {
                if (value != InternalTableEntity.PartitionKey)
                {
                    InternalTableEntity.PartitionKey = value;
                }
            }
        }

        public string Id
        {
            get => InternalTableEntity.RowKey;

            set
            {
                if (value != InternalTableEntity.RowKey)
                {
                    InternalTableEntity.RowKey = value;
                }
            }
        }

        public DateTimeOffset? LastModified
        {
            get => InternalTableEntity.Timestamp;

            set
            {
                if (value != InternalTableEntity.Timestamp)
                {
                    InternalTableEntity.Timestamp = value;
                }
            }
        }

        public ETag ETag
        {
            get => InternalTableEntity.ETag;

            set
            {
                if (value != InternalTableEntity.ETag)
                {
                    InternalTableEntity.ETag = value;
                }
            }
        }

        public virtual TableEntity ToTableEntity()
        {
            return InternalTableEntity;
        }
    }
}
