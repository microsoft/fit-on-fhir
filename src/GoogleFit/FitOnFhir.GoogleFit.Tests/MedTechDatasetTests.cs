// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text;
using Azure.Messaging.EventHubs;
using Microsoft.Health.FitOnFhir.GoogleFit.Common;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Tests
{
    public class MedTechDatasetTests
    {
        [Fact]
        public void GivenUserIdIsNull_WhenToEventDataCalled_ArgumentNullExceptionThrown()
        {
            var dataset = Data.GetMedTechDataset();
            Assert.Throws<ArgumentNullException>(() => dataset.ToEventData(null));
        }

        [Fact]
        public void GivenDeviceUidAndApplicationPackageNameAreNull_WhenToEventDataCalled_ArgumentNullExceptionThrown()
        {
            var dataset = Data.GetMedTechDataset(null, null);
            Assert.Throws<InvalidOperationException>(() => dataset.ToEventData(Data.UserId));
        }

        [Fact]
        public void GivenDeviceUidAndApplicationPackageNameAreEmpty_WhenToEventDataCalled_ArgumentNullExceptionThrown()
        {
            var dataset = Data.GetMedTechDataset(string.Empty, string.Empty);
            Assert.Throws<InvalidOperationException>(() => dataset.ToEventData(Data.UserId));
        }

        [Fact]
        public void GivenUserIdAndDeviceUid_WhenToEventDataCalled_TheEventDataContainsPatientAndDeviceIdentifiers()
        {
            var dataset = Data.GetMedTechDataset(packageName: string.Empty);
            EventData eventData = dataset.ToEventData(Data.UserId);
            JObject json = JObject.Parse(Encoding.UTF8.GetString(eventData.EventBody));

            Assert.Equal(Data.UserId, json[GoogleFitConstants.PatientIdentifier]);
            Assert.Equal("TestUserId.TestDeviceUid", json[GoogleFitConstants.DeviceIdentifier]);
        }

        [Fact]
        public void GivenUserIdAndPackageName_WhenToEventDataCalled_TheEventDataContainsPatientAndDeviceIdentifiers()
        {
            var dataset = Data.GetMedTechDataset(deviceUid: string.Empty);
            EventData eventData = dataset.ToEventData(Data.UserId);
            JObject json = JObject.Parse(Encoding.UTF8.GetString(eventData.EventBody));

            Assert.Equal(Data.UserId, json[GoogleFitConstants.PatientIdentifier]);
            Assert.Equal($"{Data.UserId}.{Data.PackageName}", json[GoogleFitConstants.DeviceIdentifier]);
        }

        [Fact]
        public void GivenUserIdDeviceUidAndPackageName_WhenToEventDataCalled_TheEventDataContainsPatientAndDeviceIdentifiers()
        {
            var dataset = Data.GetMedTechDataset();
            EventData eventData = dataset.ToEventData(Data.UserId);
            JObject json = JObject.Parse(Encoding.UTF8.GetString(eventData.EventBody));

            Assert.Equal(Data.UserId, json[GoogleFitConstants.PatientIdentifier]);
            Assert.Equal($"{Data.UserId}.{Data.PackageName}.{Data.DeviceUid}", json[GoogleFitConstants.DeviceIdentifier]);
        }

        [Fact]
        public void GivenAllRequiredPreconditionsMet_WhenToEventDataCalled_TheEventDataContainsDataset()
        {
            var dataset = Data.GetMedTechDataset();
            EventData eventData = dataset.ToEventData(Data.UserId);
            JObject json = JObject.Parse(Encoding.UTF8.GetString(eventData.EventBody));

            Assert.Equal(Data.DataSourceId, json["dataSourceId"]);
            Assert.Equal(1649715998308000000, json["minStartTimeNs"]);
            Assert.Equal(1652307998308000000, json["maxEndTimeNs"]);
            Assert.Single(json["point"]);
            Assert.Equal(165213768021173708, json["point"][0]["startTimeNanos"]);
            Assert.Equal(165213768021173708, json["point"][0]["endTimeNanos"]);
            Assert.Equal(Data.DataTypeName, json["point"][0]["dataTypeName"]);
            Assert.Equal(1652137680539, json["point"][0]["modifiedTimeMillis"]);
            Assert.Single(json["point"][0]["value"]);
            Assert.Equal(60, json["point"][0]["value"][0]["fpVal"]);
        }

        [Fact]
        public void GivenOneDataPoint_WhenGetMaxStartTimeCalled_LatestStartDateReturned()
        {
            var dataset = Data.GetMedTechDataset();
            long maxStartTime = dataset.GetMaxEndTimeNanos();
            Assert.Equal(165213768021173708, maxStartTime);
        }

        [Fact]
        public void GivenMultipleDataPoints_WhenGetMaxStartTimeCalled_LatestStartDateReturned()
        {
            var dataset = Data.GetMedTechDataset(pointCount: 2);
            long maxStartTime = dataset.GetMaxEndTimeNanos();
            Assert.Equal(165213778021173708, maxStartTime);
        }
    }
}
