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
    public enum MealType
    {
        UNSPECIFIED = 0,
        MEAL_TYPE_UNKNOWN = 1, // Unknown
        MEAL_TYPE_BREAKFAST = 2, // Breakfast
        MEAL_TYPE_LUNCH = 3, // Lunch
        MEAL_TYPE_DINNER = 4, // Dinner
        MEAL_TYPE_SNACK = 5, // Snack
    }
}
