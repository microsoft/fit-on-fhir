// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Fitness.v1.Data;
using Newtonsoft.Json;

namespace GoogleFitOnFhir.Clients.GoogleFit.Models
{
    public class IomtDataset : Dataset
    {
        public IomtDataset(Dataset dataset)
                : base()
        {
            DataSourceId = dataset.DataSourceId;
            MaxEndTimeNs = dataset.MaxEndTimeNs;
            MinStartTimeNs = dataset.MinStartTimeNs;
            NextPageToken = dataset.NextPageToken;
            Point = new List<IomtDataPoint>(dataset.Point.Select(dp =>
            {
                DateTime dateTime = new DateTime(1970, 1, 1).AddTicks(dp.EndTimeNanos.Value / 100);
                return new IomtDataPoint
                {
                    ComputationTimeMillis = dp.ComputationTimeMillis,
                    DataTypeName = dp.DataTypeName,
                    EndTimeNanos = dp.EndTimeNanos,
                    ModifiedTimeMillis = dp.ModifiedTimeMillis,
                    RawTimestampNanos = dp.RawTimestampNanos,
                    StartTimeNanos = dp.StartTimeNanos,
                    Value = dp.Value,
                    ETag = dp.ETag,
                    EndTimeISO8601 = dateTime.ToString("o"),
                };
            }));
            ETag = dataset.ETag;
        }

        [JsonProperty("userId")]
        public virtual string UserId { get; set; }

        [JsonProperty("point")]
        public new virtual IList<IomtDataPoint> Point { get; set; }
    }
}
