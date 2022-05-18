// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Azure.Messaging.EventHubs;
using FitOnFhir.GoogleFit.Clients.GoogleFit.Models;
using FitOnFhir.GoogleFit.Common;
using Google.Apis.Fitness.v1.Data;
using Newtonsoft.Json.Linq;
using Xunit;
using DataSource = FitOnFhir.GoogleFit.Clients.GoogleFit.Models.DataSource;

namespace GoogleFitOnFhir.UnitTests
{
    public class MedTechDatasetTests
    {
        private const string UserId = "TestUserId";
        private const string DeviceUid = "TestDeviceUid";
        private const string PackageName = "TestApplicationPackageName";
        private const string DataSourceId = "test:com.google.heart_rate.bpm:com.google.android.apps.fitness:user_input";

        [Fact]
        public void GivenUserIdIsNull_WhenToEventDataCalled_ArgumentNullExceptionThrown()
        {
            var dataset = GetMedTechDataset();
            Assert.Throws<ArgumentNullException>(() => dataset.ToEventData(null));
        }

        [Fact]
        public void GivenDeviceUidAndApplicationPackageNameAreNull_WhenToEventDataCalled_ArgumentNullExceptionThrown()
        {
            var dataset = GetMedTechDataset(null, null);
            Assert.Throws<InvalidOperationException>(() => dataset.ToEventData(UserId));
        }

        [Fact]
        public void GivenDeviceUidAndApplicationPackageNameAreEmpty_WhenToEventDataCalled_ArgumentNullExceptionThrown()
        {
            var dataset = GetMedTechDataset(string.Empty, string.Empty);
            Assert.Throws<InvalidOperationException>(() => dataset.ToEventData(UserId));
        }

        [Fact]
        public void GivenUserIdAndDeviceUid_WhenToEventDataCalled_TheEventDataContainsPatientandDeviceIdentifiers()
        {
            var dataset = GetMedTechDataset(packageName: string.Empty);
            EventData eventData = dataset.ToEventData(UserId);
            JObject json = JObject.Parse(Encoding.UTF8.GetString(eventData.EventBody));

            Assert.Equal(UserId, json[Constants.PatientIdentifier]);
            Assert.Equal("TestUserId.TestDeviceUid", json[Constants.DeviceIdentifier]);
        }

        [Fact]
        public void GivenUserIdAndPackageName_WhenToEventDataCalled_TheEventDataContainsPatientandDeviceIdentifiers()
        {
            var dataset = GetMedTechDataset(deviceUid: string.Empty);
            EventData eventData = dataset.ToEventData(UserId);
            JObject json = JObject.Parse(Encoding.UTF8.GetString(eventData.EventBody));

            Assert.Equal(UserId, json[Constants.PatientIdentifier]);
            Assert.Equal($"{UserId}.{PackageName}", json[Constants.DeviceIdentifier]);
        }

        [Fact]
        public void GivenUserIdDeviceUidAndPackageName_WhenToEventDataCalled_TheEventDataContainsPatientandDeviceIdentifiers()
        {
            var dataset = GetMedTechDataset();
            EventData eventData = dataset.ToEventData(UserId);
            JObject json = JObject.Parse(Encoding.UTF8.GetString(eventData.EventBody));

            Assert.Equal(UserId, json[Constants.PatientIdentifier]);
            Assert.Equal($"{UserId}.{PackageName}.{DeviceUid}", json[Constants.DeviceIdentifier]);
        }

        [Fact]
        public void GivenAllRequiredPreconditionsMet_WhenToEventDataCalled_TheEventDataContainsDataset()
        {
            var dataset = GetMedTechDataset();
            EventData eventData = dataset.ToEventData(UserId);
            JObject json = JObject.Parse(Encoding.UTF8.GetString(eventData.EventBody));

            Assert.Equal(DataSourceId, json["dataSourceId"]);
            Assert.Equal(1649715998308000000, json["minStartTimeNs"]);
            Assert.Equal(1652307998308000000, json["maxEndTimeNs"]);
            Assert.Single(json["point"]);
            Assert.Equal(165213768021173708, json["point"][0]["startTimeNanos"]);
            Assert.Equal(165213768021173708, json["point"][0]["endTimeNanos"]);
            Assert.Equal("com.google.heart_rate.bpm", json["point"][0]["dataTypeName"]);
            Assert.Equal(1652137680539, json["point"][0]["modifiedTimeMillis"]);
            Assert.Single(json["point"][0]["value"]);
            Assert.Equal(61.557689666748047, json["point"][0]["value"][0]["fpVal"]);
        }

        private MedTechDataset GetMedTechDataset(string deviceUid = DeviceUid, string packageName = PackageName)
        {
            var dataPoint = new DataPoint
            {
                Value = new List<Value>() { new Value { FpVal = 61.557689666748047 } },
                StartTimeNanos = 165213768021173708,
                EndTimeNanos = 165213768021173708,
                DataTypeName = "com.google.heart_rate.bpm",
                ModifiedTimeMillis = 1652137680539,
            };

            var dataset = new Dataset
            {
                DataSourceId = DataSourceId,
                Point = new List<DataPoint>() { dataPoint },
                MinStartTimeNs = 1649715998308000000,
                MaxEndTimeNs = 1652307998308000000,
            };

            var dataSource = new DataSource(DataSourceId, deviceUid, packageName);

            return new MedTechDataset(dataset, dataSource);
        }
    }
}
