// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.GoogleFit.Common;

namespace FitOnFhir.GoogleFit.Client.Config
{
    public class GoogleFitAuthorizationConfiguration
    {
        private IEnumerable<string> _defaultScopes;

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string CallbackUri
        {
            get
            {
                string hostName = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");
                return $"https://{hostName}/{GoogleFitConstants.GoogleFitCallbackRequest}";
            }
        }

        public IEnumerable<string> DefaultScopes
        {
            get => _defaultScopes;
            set
            {
                string[] scopes = value.First().Split(',');
                _defaultScopes = (scopes ?? Array.Empty<string>()).ToList();
            }
        }
    }
}