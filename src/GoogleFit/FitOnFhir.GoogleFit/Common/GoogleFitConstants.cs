// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.FitOnFhir.Common.Models;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Common
{
    /// <summary>
    /// Holds project wide constant strings for platform endpoints and names
    /// </summary>
    public static class GoogleFitConstants
    {
        /// <summary>
        /// GoogleFit authorize endpoint
        /// </summary>
        public const string GoogleFitAuthorizeRequest = "api/googlefit/authorize";

        /// <summary>
        /// GoogleFit callback endpoint
        /// </summary>
        public const string GoogleFitCallbackRequest = "api/googlefit/callback";

        /// <summary>
        /// GoogleFit revoke access endpoint
        /// </summary>
        public const string GoogleFitRevokeAccessRequest = "api/googlefit/revoke";

        /// <summary>
        /// String identifier for the GoogleFit platform.  Used to help identify the platform to import from, in a <see cref="QueueMessage"/>.
        /// </summary>
        public const string GoogleFitPlatformName = "GoogleFit";

        /// <summary>
        /// String identifier for the GoogleFit platform partition key in the storage account table.
        /// </summary>
        public const string GoogleFitPartitionKey = "GoogleFit";

        /// <summary>
        /// The key in a JSON payload sent to an Event Hub for a globally unique patient identifier.
        /// </summary>
        public const string PatientIdentifier = "patientIdentifier";

        /// <summary>
        /// The key in a JSON payload sent to an Event Hub for a globally unique device identifier.
        /// </summary>
        public const string DeviceIdentifier = "deviceIdentifier";
    }
}
