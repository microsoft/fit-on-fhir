// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Health.FitOnFhir.Common.Models
{
    public class PlatformUserInfo : IEquatable<PlatformUserInfo>
    {
        public PlatformUserInfo(string platformName, string userId, DataImportState dataImportState)
        {
            PlatformName = EnsureArg.IsNotNullOrWhiteSpace(platformName, nameof(platformName));
            UserId = EnsureArg.IsNotNullOrWhiteSpace(userId, nameof(userId));
            ImportState = dataImportState;
        }

        public string PlatformName { get; set; }

        public string UserId { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public DataImportState ImportState { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public RevokeReason RevokedAccessReason { get; set; }

        public DateTimeOffset? RevokedTimeStamp { get; set; }

        public override bool Equals(object other)
        {
            if (other is PlatformUserInfo platformUserInfo)
            {
                return Equals(platformUserInfo);
            }

            return false;
        }

        public bool Equals(PlatformUserInfo other)
        {
            if (other != null)
            {
                return PlatformName == other.PlatformName &&
                   UserId == other.UserId &&
                   ImportState == other.ImportState &&
                   RevokedAccessReason == other.RevokedAccessReason &&
                   RevokedTimeStamp == other.RevokedTimeStamp;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return PlatformName.GetHashCode(StringComparison.Ordinal) ^
                   UserId.GetHashCode(StringComparison.Ordinal) ^
                   ImportState.GetHashCode() ^
                   RevokedAccessReason.GetHashCode() ^
                   RevokedTimeStamp.GetHashCode();
        }
    }
}
