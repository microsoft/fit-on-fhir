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

            // these are not the final default values
            DefaultScopes = new[]
            {
                "https://www.googleapis.com/auth/userinfo.email",
                "https://www.googleapis.com/auth/userinfo.profile",
                FitnessService.Scope.FitnessActivityRead,
                FitnessService.Scope.FitnessSleepRead,
                FitnessService.Scope.FitnessReproductiveHealthRead,
                FitnessService.Scope.FitnessOxygenSaturationRead,
                FitnessService.Scope.FitnessNutritionRead,
                FitnessService.Scope.FitnessLocationRead,
                FitnessService.Scope.FitnessBodyTemperatureRead,
                FitnessService.Scope.FitnessBodyRead,
                FitnessService.Scope.FitnessBloodPressureRead,
                FitnessService.Scope.FitnessBloodGlucoseRead,
                FitnessService.Scope.FitnessHeartRateRead,
            };
        }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string CallbackUri { get; set; }

        public IEnumerable<string> DefaultScopes { get; private set; }
    }
}