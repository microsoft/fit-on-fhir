// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace GoogleFitOnFhir.Clients.GoogleFit.Responses
{
    public class AuthTokensResponse
    {
        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

        public string IdToken { get; set; }
    }
}