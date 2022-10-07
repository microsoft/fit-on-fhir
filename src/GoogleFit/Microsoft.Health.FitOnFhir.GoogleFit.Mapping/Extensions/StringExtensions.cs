// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.Utility;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Mapping.Extensions
{
    internal static class StringExtensions
    {
        internal static string UnderscoreToCamelCase(this string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                string[] words = text.Split('_');
                IEnumerable<string> capitalizedWords = words.Select(w => w.Capitalize());
                return string.Join(string.Empty, capitalizedWords);
            }

            return text;
        }
    }
}
