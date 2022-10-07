// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.FitOnFhir.GoogleFit.Mapping.Enums
{
    /// <summary>
    /// https://developers.google.com/fit/datatypes/health#blood_glucose
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Matching Google formatting")]
    public enum BloodGlucoseSpecimenSource
    {
        UNSPECIFIED = 0,
        BLOOD_GLUCOSE_SPECIMEN_SOURCE_INTERSTITIAL_FLUID = 1, // Interstitial fluid
        BLOOD_GLUCOSE_SPECIMEN_SOURCE_CAPILLARY_BLOOD = 2, // Capillary blood
        BLOOD_GLUCOSE_SPECIMEN_SOURCE_PLASMA = 3, // Plasma
        BLOOD_GLUCOSE_SPECIMEN_SOURCE_SERUM = 4, // Serum
        BLOOD_GLUCOSE_SPECIMEN_SOURCE_TEARS = 5, // Tears
        BLOOD_GLUCOSE_SPECIMEN_SOURCE_WHOLE_BLOOD = 6, // Whole blood
    }
}
