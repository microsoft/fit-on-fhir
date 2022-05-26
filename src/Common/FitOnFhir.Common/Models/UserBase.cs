// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Azure.Data.Tables;

namespace FitOnFhir.Common.Models
{
    public abstract class UserBase : ITableEntity
    {
        private readonly TableEntity _tableEntity;

        public UserBase(string partitionKey, string rowKey)
        {
            _tableEntity = new TableEntity(partitionKey, rowKey);
        }

        public string PartitionKey
        {
            get => _tableEntity.PartitionKey;

            set
            {
                if (value != _tableEntity.PartitionKey)
                {
                    _tableEntity.PartitionKey = value;
                }
            }
        }

        public string RowKey
        {
            get => _tableEntity.RowKey;

            set
            {
                if (value != _tableEntity.RowKey)
                {
                    _tableEntity.RowKey = value;
                }
            }
        }

        public DateTimeOffset? Timestamp
        {
            get => _tableEntity.Timestamp;

            set
            {
                if (value != _tableEntity.Timestamp)
                {
                    _tableEntity.Timestamp = value;
                }
            }
        }

        public ETag ETag
        {
            get => _tableEntity.ETag;

            set
            {
                if (value != _tableEntity.ETag)
                {
                    _tableEntity.ETag = value;
                }
            }
        }

        public TableEntity Entity => _tableEntity;
    }
}
