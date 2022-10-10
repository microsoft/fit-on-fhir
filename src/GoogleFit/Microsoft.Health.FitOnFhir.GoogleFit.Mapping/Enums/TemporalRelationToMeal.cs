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
    public enum TemporalRelationToMeal
    {
        UNSPECIFIED = 0,
        FIELD_TEMPORAL_RELATION_TO_MEAL_GENERAL = 1, // Reading wasn't taken before or after a meal
        FIELD_TEMPORAL_RELATION_TO_MEAL_FASTING = 2, // Reading was taken during a fasting period
        FIELD_TEMPORAL_RELATION_TO_MEAL_BEFORE_MEAL = 3, // Reading was taken before a meal
        FIELD_TEMPORAL_RELATION_TO_MEAL_AFTER_MEAL = 4, // Reading was taken after a meal
    }
}
