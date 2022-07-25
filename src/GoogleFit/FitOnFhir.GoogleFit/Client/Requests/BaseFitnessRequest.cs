// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Google.Apis.Auth.OAuth2;
using Google.Apis.Fitness.v1;
using Google.Apis.Services;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Client.Requests
{
    public class BaseFitnessRequest
    {
        public BaseFitnessRequest(string accessToken)
        {
            AccessToken = accessToken;
            FitnessService = new FitnessService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = GoogleCredential.FromAccessToken(accessToken),
            });
        }

        protected FitnessService FitnessService { get; }

        protected string AccessToken { get; }
    }
}