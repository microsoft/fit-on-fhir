// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.GoogleFit.Client.Models;
using Google.Apis.Fitness.v1.Data;

namespace FitOnFhir.GoogleFit.Tests
{
    public static class Data
    {
        public const string UserId = "TestUserId";
        public const string DeviceUid = "TestDeviceUid";
        public const string PackageName = "TestApplicationPackageName";
        public const string DataSourceId = "test:com.google.heart_rate.bpm:com.google.android.apps.fitness:user_input";
        public const string DataTypeName = "com.google.heart_rate.bpm";

        public static MedTechDataset GetMedTechDataset(string deviceUid = DeviceUid, string packageName = PackageName, int pointCount = 1)
        {
            var points = new List<DataPoint>();

            for (int i = 0; i < pointCount; i++)
            {
                points.Add(new DataPoint
                {
                    Value = new List<Value>() { new Value { FpVal = 60 + i } },
                    StartTimeNanos = 165213768021173708 + (i * 10000000000),
                    EndTimeNanos = 165213768021173708 + (i * 10000000000),
                    DataTypeName = DataTypeName,
                    ModifiedTimeMillis = 1652137680539 + (i * 1000),
                });
            }

            var dataset = new Dataset
            {
                DataSourceId = DataSourceId,
                Point = points,
                MinStartTimeNs = 1649715998308000000,
                MaxEndTimeNs = 1652307998308000000,
            };

            var dataSource = new Client.Models.DataSource(DataSourceId, deviceUid, packageName);

            return new MedTechDataset(dataset, dataSource);
        }
    }
}
