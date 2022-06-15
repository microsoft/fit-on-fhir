// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace FitOnFhir.Common.Models
{
    public class PlatformUserInfo
    {
        public PlatformUserInfo(string platformName, string userId, DataImportState dataImportState)
        {
            PlatformName = EnsureArg.IsNotEmptyOrWhiteSpace(platformName, nameof(platformName));
            UserId = EnsureArg.IsNotEmptyOrWhiteSpace(userId, nameof(userId));
            ImportState = dataImportState;
        }

        public string PlatformName { get; set; }

        public string UserId { get; set; }

        public DataImportState ImportState { get; set; }
    }
}
