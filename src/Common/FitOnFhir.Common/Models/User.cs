// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Data.Tables;

namespace FitOnFhir.Common.Models
{
    public class User : UserBase
    {
        public User(Guid userId)
            : base(Constants.UsersPartitionKey, userId.ToString())
        {
        }

        public User()
            : base(string.Empty, string.Empty)
        {
        }

        public DateTimeOffset? LastTouched { get; set; }

        /// <summary>
        /// A <see cref="TableEntity"/> that stores any platform names (key) for this user along with their user ID for that platform (value)
        /// </summary>
        public TableEntity PlatformUserInfo => Entity;
    }
}
