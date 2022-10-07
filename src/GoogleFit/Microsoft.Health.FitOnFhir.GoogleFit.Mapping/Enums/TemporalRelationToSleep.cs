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
    public enum TemporalRelationToSleep
    {
        UNSPECIFIED = 0,
        TEMPORAL_RELATION_TO_SLEEP_FULLY_AWAKE = 1, // User was fully awake.
        TEMPORAL_RELATION_TO_SLEEP_BEFORE_SLEEP = 2, // Before the user fell asleep.
        TEMPORAL_RELATION_TO_SLEEP_ON_WAKING = 3, // After the user woke up.
        TEMPORAL_RELATION_TO_SLEEP_DURING_SLEEP = 4, // While the user was still sleeping.
    }
}
