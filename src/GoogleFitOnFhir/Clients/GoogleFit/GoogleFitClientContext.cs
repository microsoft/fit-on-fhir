// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text;
using Google.Apis.Fitness.v1;

namespace GoogleFitOnFhir.Clients.GoogleFit
{
    public class GoogleFitClientContext
    {
        public GoogleFitClientContext(string clientId, string clientSecret, string hostName)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;

            StringBuilder stringBuilder = new StringBuilder("https");
            stringBuilder.Append("://")
                .Append(hostName)
                .Append("/api/googlefit/callback");

            CallbackUri = stringBuilder.ToString();

            DefaultScopes = new[]
            {
                "https://www.googleapis.com/auth/userinfo.email",
                "https://www.googleapis.com/auth/userinfo.profile",
                FitnessService.Scope.FitnessBloodGlucoseRead,
                FitnessService.Scope.FitnessBloodGlucoseWrite,
                FitnessService.Scope.FitnessHeartRateRead,
                FitnessService.Scope.FitnessHeartRateWrite,
            };
        }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string CallbackUri { get; set; }

        public IEnumerable<string> DefaultScopes { get; private set; }
    }
}