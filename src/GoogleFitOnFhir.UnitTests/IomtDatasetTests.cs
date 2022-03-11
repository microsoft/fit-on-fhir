// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Google.Apis.Fitness.v1.Data;
using GoogleFitOnFhir.Clients.GoogleFit.Models;
using Xunit;

namespace GoogleFitOnFhir.UnitTests
{
    public class IomtDatasetTests
    {
        [Fact]
        public void TestIomtDatasetConvertsDataset()
        {
            long nanos = 1628009728000000000;
            Dataset dataset = new Dataset
            {
                DataSourceId = "dataSourceId",
                MaxEndTimeNs = 12345,
                MinStartTimeNs = 12345,
                NextPageToken = "token",
                Point = new List<DataPoint>()
                {
                    new DataPoint
                    {
                        ComputationTimeMillis = 12345,
                        DataTypeName = "test",
                        EndTimeNanos = nanos,
                        ModifiedTimeMillis = 12345,
                        OriginDataSourceId = "test",
                        RawTimestampNanos = 12345,
                        StartTimeNanos = 12345,
                        Value = new List<Value>()
                        {
                            new Value
                            {
                                FpVal = 5,
                                MapVal = new List<ValueMapValEntry>() { },
                            },
                        },
                        ETag = "etag",
                    },
                },
            };

            IomtDataset iomtDataset = new IomtDataset(dataset);

            Assert.Equal("2021-08-03T16:55:28.0000000", iomtDataset.Point[0].EndTimeISO8601);

            Assert.Equal(dataset.DataSourceId, iomtDataset.DataSourceId);
            Assert.Equal(dataset.MaxEndTimeNs, iomtDataset.MaxEndTimeNs);
            Assert.Equal(dataset.MinStartTimeNs, iomtDataset.MinStartTimeNs);
            Assert.Equal(dataset.NextPageToken, iomtDataset.NextPageToken);
            Assert.Equal(dataset.ETag, iomtDataset.ETag);
        }
    }
}
