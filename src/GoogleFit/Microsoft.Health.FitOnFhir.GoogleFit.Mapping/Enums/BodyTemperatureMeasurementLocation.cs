// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.FitOnFhir.GoogleFit.Mapping.Enums
{
    /// <summary>
    /// https://developers.google.com/fit/datatypes/health#body_temperature
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Matching Google formatting")]
    public enum BodyTemperatureMeasurementLocation
    {
        UNSPECIFIED = 0,
        BODY_TEMPERATURE_MEASUREMENT_LOCATION_AXILLARY = 1, // Armpit
        BODY_TEMPERATURE_MEASUREMENT_LOCATION_FINGER = 2, // Finger
        BODY_TEMPERATURE_MEASUREMENT_LOCATION_FOREHEAD = 3, // Forehead
        BODY_TEMPERATURE_MEASUREMENT_LOCATION_ORAL = 4, // Mouth (oral)
        BODY_TEMPERATURE_MEASUREMENT_LOCATION_RECTAL = 5, // Rectum
        BODY_TEMPERATURE_MEASUREMENT_LOCATION_TEMPORAL_ARTERY = 6, // Temporal artery
        BODY_TEMPERATURE_MEASUREMENT_LOCATION_TOE = 7, // Toe
        BODY_TEMPERATURE_MEASUREMENT_LOCATION_TYMPANIC = 8, // Ear (tympanic
        BODY_TEMPERATURE_MEASUREMENT_LOCATION_WRIST = 9, // Wrist
        BODY_TEMPERATURE_MEASUREMENT_LOCATION_VAGINAL = 10, // Vagina
    }
}
