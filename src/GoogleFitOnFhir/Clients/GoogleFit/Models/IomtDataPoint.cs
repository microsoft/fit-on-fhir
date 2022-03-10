// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Google.Apis.Fitness.v1.Data;
using Newtonsoft.Json;

namespace GoogleFitOnFhir.Clients.GoogleFit.Models
{
    public class IomtDataPoint : DataPoint
    {
        [JsonProperty("endTimeISO8601")]
        public virtual string EndTimeISO8601 { get; set; }
    }
}
