// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.FitOnFhir.Common.Models
{
    public class QueueMessage
    {
        public QueueMessage(string userId, string platformUserId, string platformName)
        {
            UserId = EnsureArg.IsNotNullOrWhiteSpace(userId);
            PlatformUserId = EnsureArg.IsNotNullOrWhiteSpace(platformUserId);
            PlatformName = EnsureArg.IsNotNullOrWhiteSpace(platformName);
        }

        public string UserId { get; set; }

        public string PlatformUserId { get; set; }

        public string PlatformName { get; set; }
    }
}
