// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.FitOnFhir.Common.Models
{
    public enum DataImportState
    {
        /// <summary>
        /// Indicates that the data for this platform is ready to be synced
        /// </summary>
        ReadyToImport,

        /// <summary>
        /// Indicates that the data for this platform is queued for importing
        /// </summary>
        Queued,

        /// <summary>
        /// Indicates that the data for this platform is currently being imported
        /// </summary>
        Importing,

        /// <summary>
        /// Indicates that this platform is no longer authorized to import data for this user
        /// </summary>
        Unauthorized,
    }
}
