// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.FitOnFhir.GoogleFit.Mapping.Enums
{
    /// <summary>
    /// https://developers.google.com/fit/datatypes/health#oxygen_saturation
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Matching Google formatting")]
    public enum OxygenTherapyAdministrationMode
    {
        UNSPECIFIED = 0,
        OXYGEN_THERAPY_ADMINISTRATION_MODE_NASAL_CANULA = 1,
    }
}
