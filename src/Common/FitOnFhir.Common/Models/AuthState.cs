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

        public AuthState(
            string externalIdentifier,
            string externalSystem,
            DateTimeOffset expirationTimeStamp,
            Uri redirectUrl,
            string state = null)
        {
            ExternalIdentifier = EnsureArg.IsNotNullOrWhiteSpace(externalIdentifier, nameof(externalIdentifier));
            ExternalSystem = EnsureArg.IsNotNullOrWhiteSpace(externalSystem, nameof(externalSystem));
            ExpirationTimeStamp = expirationTimeStamp;
            RedirectUrl = EnsureArg.IsNotNull(redirectUrl, nameof(redirectUrl));
            State = state;
        }

        [JsonProperty(Constants.ExternalIdQueryParameter)]
        [JsonConverter(typeof(UrlSafeJsonConverter))]
        [JsonRequired]
        public string ExternalIdentifier { get; set; }

        [JsonProperty(Constants.ExternalSystemQueryParameter)]
        [JsonConverter(typeof(UrlSafeJsonConverter))]
        [JsonRequired]
        public string ExternalSystem { get; set; }

        [JsonProperty(Constants.RedirectUrlQueryParameter)]
        [JsonRequired]
        public Uri RedirectUrl { get; set; }

        [JsonProperty(Constants.StateQueryParameter)]
        [JsonConverter(typeof(UrlSafeJsonConverter))]
        public string State { get; set; }

        public DateTimeOffset ExpirationTimeStamp { get; set; }

        public static AuthState Parse(string jsonString)
        {
            EnsureArg.IsNotNullOrWhiteSpace(jsonString, nameof(jsonString));
            AuthState authState = JsonConvert.DeserializeObject<AuthState>(jsonString);

            if (string.IsNullOrEmpty(authState.ExternalIdentifier) ||
                string.IsNullOrEmpty(authState.ExternalSystem) ||
                authState.RedirectUrl == default ||
                authState.ExpirationTimeStamp == default)
            {
                throw new ArgumentException($"{nameof(jsonString)} is missing a required property ({nameof(authState.ExternalIdentifier)}, {nameof(authState.ExternalSystem)}, {nameof(authState.RedirectUrl)} or {nameof(authState.ExpirationTimeStamp)}).");
            }

            return authState;
        }
    }
}
