// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.FitOnFhir.GoogleFit.Common;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Client.Config
{
    public class GoogleFitAuthorizationConfiguration
    {
        private string[] _scopes;

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

        public IEnumerable<string> AuthorizedScopes => _scopes;

        public string Scopes
        {
            get
            {
                if (_scopes != null && _scopes.Any())
                {
                    return string.Join(", ", _scopes);
                }

                return null;
            }

            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _scopes = value.Replace(" ", string.Empty).Split(',');
                    return;
                }
            }
        }
    }
}