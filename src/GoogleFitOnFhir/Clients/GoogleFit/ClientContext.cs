// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace GoogleFitOnFhir.Clients.GoogleFit
{
    public class ClientContext
    {
        public ClientContext(string clientId, string clientSecret, string callbackUri)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
            CallbackUri = callbackUri;
        }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string CallbackUri { get; set; }
    }
}