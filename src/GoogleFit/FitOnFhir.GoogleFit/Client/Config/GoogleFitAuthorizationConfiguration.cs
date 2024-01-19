// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.FitOnFhir.GoogleFit.Common;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Client.Config
{
    public class GoogleFitAuthorizationConfiguration
    {
        private readonly string _hostName;
        private string[] _scopes;

        public GoogleFitAuthorizationConfiguration()
        {
            _hostName = EnsureArg.IsNotNullOrWhiteSpace(Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME"), "WEBSITE_HOSTNAME");
        }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public Uri CallbackUri => new Uri($"https://{_hostName}/{GoogleFitConstants.GoogleFitCallbackRequest}");

        public IEnumerable<string> AuthorizedScopes => _scopes;

        public string Scopes
        {
            get
            {
                if (_scopes != null && _scopes.Length > 0)
                {
                    return string.Join(", ", _scopes);
                }

                return null;
            }

            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _scopes = value.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase).Split(',');
                    return;
                }
            }
        }
    }
}
