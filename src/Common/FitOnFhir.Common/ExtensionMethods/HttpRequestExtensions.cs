// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.FitOnFhir.Common.ExtensionMethods
{
    public static class HttpRequestExtensions
    {
        public static bool TryGetTokenStringFromAuthorizationHeader(this HttpRequest request, string scheme, out string token)
        {
            token = null;
            return request?.Headers != null &&
                   request.Headers.TryGetValue(HeaderNames.Authorization, out var authorizationHeaderValues) &&
                   authorizationHeaderValues.Count == 1 &&
                   TryGetTokenStringWithMatchingScheme(authorizationHeaderValues.ToString(), scheme, out token);
        }

        public static bool TryGetTokenStringFromAuthorizationHeader(this HttpRequestHeaders requestHeaders, string scheme, out string token)
        {
            token = null;
            return requestHeaders != null &&
                   requestHeaders.TryGetValues(HeaderNames.Authorization, out var authorizationHeaderValues) &&
                   authorizationHeaderValues.Count() == 1 &&
                   TryGetTokenStringWithMatchingScheme(authorizationHeaderValues.First(), scheme, out token);
        }

        private static bool TryGetTokenStringWithMatchingScheme(string authorizationHeaderValue, string scheme, out string token)
        {
            Match match = Regex.Match(authorizationHeaderValue, $"^\\s*{scheme}\\s+(\\S*)", RegexOptions.IgnoreCase);
            token = match.Success ? match.Groups[1].Value : null;
            return match.Success;
        }
    }
}
