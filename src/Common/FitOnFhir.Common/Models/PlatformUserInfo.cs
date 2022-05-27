// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace FitOnFhir.Common.Models
{
    public class PlatformUserInfo
    {
        public PlatformUserInfo(string platformName, string platformUserId)
        {
            PlatformName = platformName;
            PlatformUserId = platformUserId;
        }

        public string PlatformName { get; set; }

        public string PlatformUserId { get; set; }
    }
}
