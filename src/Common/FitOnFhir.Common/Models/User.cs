// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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
        /// Retrieves the user name associated with the platform name provided
        /// </summary>
        /// <param name="platformName">The platform ID associated with the user name.</param>
        /// <returns>The user name for the platform.</returns>
        public string GetPlatformUserName(string platformName)
        {
            string userId = null;

            if (Entity.TryGetValue(platformName, out object userName))
            {
                userId = userName as string;
            }

            return userId;
        }

        /// <summary>
        /// Stores the user name associated with a platform.
        /// </summary>
        /// <param name="platformName">The platform ID associated with the user name.</param>
        /// <param name="userName">The user name for this platform.</param>
        public void SavePlatformUserName(string platformName, string userName)
        {
            Entity.Add(platformName, userName);
        }

        /// <summary>
        /// Converts the underlying Dictionary from having an object value, to a string value.
        /// </summary>
        public Dictionary<string, string> ToDictionary()
        {
            return Entity.Select(t => new { t.Key, t.Value })
                .ToDictionary(t => t.Key, t => t.Value as string);
        }
    }
}
