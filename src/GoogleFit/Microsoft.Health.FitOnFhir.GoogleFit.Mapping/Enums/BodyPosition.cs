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
    public enum BodyPosition
    {
        UNSPECIFIED = 0,
        BODY_POSITION_STANDING = 1, // Standing up
        BODY_POSITION_SITTING = 2, // Sitting down
        BODY_POSITION_LYING_DOWN = 3, // Lying down
        BODY_POSITION_SEMI_RECUMBENT = 4, // Reclining
    }
}
