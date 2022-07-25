// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.FitOnFhir.GoogleFit.Client.Models
{
    public class DataSource
    {
        public DataSource(string dataStreamId, string deviceUid, string applicationPackageName)
        {
            DataStreamId = dataStreamId;
            DeviceUid = deviceUid;
            ApplicationPackageName = applicationPackageName;
        }

        public string DataStreamId { get; }

        public string DeviceUid { get; }

        public string ApplicationPackageName { get; }
    }
}
