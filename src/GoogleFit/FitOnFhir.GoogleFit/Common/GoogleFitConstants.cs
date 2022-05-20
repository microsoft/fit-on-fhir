// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.Common.Models;

namespace FitOnFhir.GoogleFit.Common
{
    /// <summary>
    /// Holds project wide constant strings for platform endpoints and names
    /// </summary>
    public class GoogleFitConstants
    {
        /// <summary>
        /// GoogleFit authorize endpoint
        /// </summary>
        public const string GoogleFitAuthorizeRequest = "auth/googlefit/authorize";

        /// <summary>
        /// GoogleFit callback endpoint
        /// </summary>
        public const string GoogleFitCallbackRequest = "auth/googlefit/callback";

        /// <summary>
        /// String identifier for the GoogleFit platform.  Used to help identify the platform to import from, in a <see cref="QueueMessage"/>.
        /// </summary>
        public const string GoogleFitPlatformName = "GoogleFit";

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
