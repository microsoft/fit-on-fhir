// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using FitOnFhir.GoogleFit.Common;
using Google.Apis.Fitness.v1;

namespace FitOnFhir.GoogleFit.Client
{
    public class GoogleFitClientContext
    {
        public GoogleFitClientContext(string clientId, string clientSecret, string hostName)
        {
            ClientId = EnsureArg.IsNotNullOrWhiteSpace(clientId, nameof(clientId));
            ClientSecret = EnsureArg.IsNotNullOrWhiteSpace(clientSecret, nameof(clientSecret));
            EnsureArg.IsNotNullOrWhiteSpace(hostName, nameof(hostName));
            CallbackUri = $"https://{hostName}/{GoogleFitConstants.GoogleFitCallbackRequest}";

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