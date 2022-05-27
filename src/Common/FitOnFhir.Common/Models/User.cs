// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FitOnFhir.Common.Models
{
    public class User : UserBase
    {
        private const string _platformsKey = "Platforms";

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
        /// <param name="platformUserInfo">Contains the platform name associated with the user ID.</param>
        public void SavePlatformUserName(PlatformUserInfo platformUserInfo)
        {
            // take the List<> passed in, and convert to json string
            var jsonString = JObject.FromObject(platformUserInfo).ToString();

            // store the json string to the _platformKeys key in the TableEntity dictionary
            Entity.Add(_platformsKey, jsonString);
        }

        /// <summary>
        /// Converts the underlying Dictionary from having an object value, to a string value.
        /// </summary>
        public Dictionary<string, string> ToDictionary()
        {
            var json = Entity[_platformsKey] as string;
            Dictionary<string, string> platformInfo = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            return platformInfo;
        }
    }
}
