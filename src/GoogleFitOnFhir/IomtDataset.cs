using System;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Fitness.v1.Data;
using Newtonsoft.Json;

namespace GoogleFitOnFhir
{
    public class IomtDataset : Dataset
    {
        public IomtDataset(Dataset dataset)
                : base()
        {
            this.DataSourceId = dataset.DataSourceId;
            this.MaxEndTimeNs = dataset.MaxEndTimeNs;
            this.MinStartTimeNs = dataset.MinStartTimeNs;
            this.NextPageToken = dataset.NextPageToken;
            this.Point = new List<IomtDataPoint>(dataset.Point.Select(dp =>
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
            this.ETag = dataset.ETag;
        }

        [JsonProperty("userId")]
        public virtual string UserId { get; set; }

        [JsonProperty("point")]
        public new virtual IList<IomtDataPoint> Point { get; set; }
    }
}
