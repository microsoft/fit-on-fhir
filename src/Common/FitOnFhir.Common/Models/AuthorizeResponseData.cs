// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.FitOnFhir.Common.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Health.FitOnFhir.Common.Models
{
    public class AuthorizeResponseData
    {
        [JsonConstructor]
        public AuthorizeResponseData()
        {
        }

        public AuthorizeResponseData(string authUrl, DateTimeOffset expiresAt)
        {
            AuthUrl = authUrl;
            ExpiresAt = expiresAt;
        }

        [JsonRequired]
        public string AuthUrl { get; set; }

        [JsonRequired]
        public DateTimeOffset ExpiresAt { get; set; }
    }
}
