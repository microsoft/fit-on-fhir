// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Fhir.Ingest.Template;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Mapping.Extensions
{
    public static class FhirValueTypeExtensions
    {
        public static IList<FhirCode> GetFhirCodes(this FhirValueType value, string prefix)
        {
            return new List<FhirCode>
            {
                new FhirCode
                {
                    Code = $"{prefix}.{value?.ValueName}",
                },
            };
        }
    }
}
