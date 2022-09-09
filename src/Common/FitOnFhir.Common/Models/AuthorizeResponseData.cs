// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Newtonsoft.Json;

namespace Microsoft.Health.FitOnFhir.Common.Models
{
    public class AuthorizeResponseData
    {
        [JsonConstructor]
        public AuthorizeResponseData()
        {
        }

        public AuthorizeResponseData(Uri authUrl, DateTimeOffset expiresAt)
        {
            AuthUrl = authUrl;
            ExpiresAt = expiresAt;
        }

        [JsonProperty("authUrl")]
        [JsonRequired]
        public Uri AuthUrl { get; set; }

        [JsonProperty("expiresAt")]
        [JsonRequired]
        public DateTimeOffset ExpiresAt { get; set; }
    }
}
