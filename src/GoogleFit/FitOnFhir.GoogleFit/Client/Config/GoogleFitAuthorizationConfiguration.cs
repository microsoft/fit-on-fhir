// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.GoogleFit.Common;

namespace FitOnFhir.GoogleFit.Client.Config
{
    public class GoogleFitAuthorizationConfiguration
    {
        private string[] _defaultScopes;

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

        public IEnumerable<string> Scopes => _defaultScopes;

        public string DefaultScopes
        {
            get
            {
                if (_defaultScopes != null && _defaultScopes.Any())
                {
                    return string.Join(", ", _defaultScopes);
                }

                return null;
            }

            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _defaultScopes = value.Replace(" ", string.Empty).Split(',');
                    return;
                }
            }
        }
    }
}