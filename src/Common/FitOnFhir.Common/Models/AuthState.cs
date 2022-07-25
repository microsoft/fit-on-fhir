// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Web;
using EnsureThat;
using Microsoft.AspNetCore.Http;
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

        public AuthState(IQueryCollection query)
        {
            PatientId = HttpUtility.UrlDecode(EnsureArg.IsNotNullOrWhiteSpace(query?[Constants.PatientIdQueryParameter], $"query.{Constants.PatientIdQueryParameter}"));
            System = HttpUtility.UrlDecode(EnsureArg.IsNotNullOrWhiteSpace(query?[Constants.SystemQueryParameter], $"query.{Constants.SystemQueryParameter}"));
        }

        [JsonProperty(Constants.PatientIdQueryParameter)]
        [JsonConverter(typeof(UrlSafeJsonConverter))]
        [JsonRequired]
        public string PatientId { get; set; }

        [JsonProperty(Constants.SystemQueryParameter)]
        [JsonConverter(typeof(UrlSafeJsonConverter))]
        [JsonRequired]
        public string System { get; set; }

        public static AuthState Parse(string jsonString)
        {
            EnsureArg.IsNotNullOrWhiteSpace(jsonString, nameof(jsonString));
            AuthState authState = JsonConvert.DeserializeObject<AuthState>(jsonString);

            if (string.IsNullOrEmpty(authState.PatientId) || string.IsNullOrEmpty(authState.System))
            {
                throw new ArgumentException();
            }

            return authState;
        }
    }
}
