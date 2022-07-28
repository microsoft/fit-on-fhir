// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.FitOnFhir.Common.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Health.FitOnFhir.Common.Models
{
    public class AuthState
    {
        [JsonConstructor]
        public AuthState()
        {
        }

        public AuthState(string externalIdentifier, string externalSystem)
        {
            ExternalIdentifier = EnsureArg.IsNotEmptyOrWhiteSpace(externalIdentifier, nameof(externalIdentifier));
            ExternalSystem = EnsureArg.IsNotEmptyOrWhiteSpace(externalSystem, nameof(externalSystem));
        }

        [JsonProperty(Constants.ExternalIdQueryParameter)]
        [JsonConverter(typeof(UrlSafeJsonConverter))]
        [JsonRequired]
        public string ExternalIdentifier { get; set; }

        [JsonProperty(Constants.ExternalSystemQueryParameter)]
        [JsonConverter(typeof(UrlSafeJsonConverter))]
        [JsonRequired]
        public string ExternalSystem { get; set; }

        public static AuthState Parse(string jsonString)
        {
            EnsureArg.IsNotNullOrWhiteSpace(jsonString, nameof(jsonString));
            AuthState authState = JsonConvert.DeserializeObject<AuthState>(jsonString);

            if (string.IsNullOrEmpty(authState.ExternalIdentifier) || string.IsNullOrEmpty(authState.ExternalSystem))
            {
                throw new ArgumentException();
            }

            return authState;
        }
    }
}
