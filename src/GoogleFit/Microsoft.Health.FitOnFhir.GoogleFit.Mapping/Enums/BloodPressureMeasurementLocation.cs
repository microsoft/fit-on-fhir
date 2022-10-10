// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.FitOnFhir.GoogleFit.Mapping.Enums
{
    /// <summary>
    /// https://developers.google.com/fit/datatypes/health#blood_pressure
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Matching Google formatting")]
    public enum BloodPressureMeasurementLocation
    {
        UNSPECIFIED = 0,
        BLOOD_PRESSURE_MEASUREMENT_LOCATION_LEFT_WRIST = 1, // Left wrist
        BLOOD_PRESSURE_MEASUREMENT_LOCATION_RIGHT_WRIST = 2, // Right wrist
        BLOOD_PRESSURE_MEASUREMENT_LOCATION_LEFT_UPPER_ARM = 3, // Left upper arm
        BLOOD_PRESSURE_MEASUREMENT_LOCATION_RIGHT_UPPER_ARM = 4, // Right upper arm
    }
}
