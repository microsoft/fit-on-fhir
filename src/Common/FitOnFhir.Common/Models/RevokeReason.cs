// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.FitOnFhir.Common.Models
{
    public enum RevokeReason
    {
        /// <summary>
        /// Indicates that the reason for access being revoked is not known
        /// </summary>
        Unknown,

        /// <summary>
        /// Indicates that the user requested that access be revoked
        /// </summary>
        UserInitiated,
    }
}
