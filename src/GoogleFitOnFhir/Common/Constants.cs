// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using GoogleFitOnFhir.Models;

namespace GoogleFitOnFhir.Common
{
    /// <summary>
    /// Holds project wide constant strings for platform endpoints and names
    /// </summary>
    public class Constants
    {
        /// <summary>
        /// GoogleFit authorize endpoint
        /// </summary>
        public static string GoogleFitAuthorizeRequest => "api/googlefit/authorize";

        /// <summary>
        /// GoogleFit callback endpoint
        /// </summary>
        public static string GoogleFitCallbackRequest => "api/googlefit/callback";

        /// <summary>
        /// String identifier for the GoogleFit platform.  Used to help identify the platform to import from, in a <see cref="QueueMessage"/>.
        /// </summary>
        public static string GoogleFitPlatformName => "GoogleFit";

        /// <summary>
        /// Sets the results limit of a Dataset request
        /// </summary>
        public static int GoogleFitDatasetRequestLimit => 10;
    }
}
