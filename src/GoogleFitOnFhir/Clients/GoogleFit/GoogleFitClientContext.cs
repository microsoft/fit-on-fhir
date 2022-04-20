// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text;

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
        }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string CallbackUri { get; set; }
    }
}