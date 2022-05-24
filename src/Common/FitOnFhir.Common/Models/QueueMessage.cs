// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace FitOnFhir.Common.Models
{
    public class QueueMessage
    {
        public QueueMessage(string userId, string platformName)
        {
            UserId = EnsureArg.IsNotNullOrWhiteSpace(userId);
            PlatformName = EnsureArg.IsNotNullOrWhiteSpace(platformName);
        }

        public string UserId { get; set; }

        public string PlatformName { get; set; }
    }
}
