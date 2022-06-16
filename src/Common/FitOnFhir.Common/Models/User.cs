// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using Azure.Data.Tables;
using EnsureThat;
using Newtonsoft.Json;

namespace FitOnFhir.Common.Models
{
    public class User : EntityBase
    {
        private const string _platformsKey = "Platforms";
        private ConcurrentDictionary<string, PlatformUserInfo> _platformUserInfo = new ConcurrentDictionary<string, PlatformUserInfo>();

        public User()
            : base(new TableEntity())
        {
        }

        public User(Guid userId)
            : this(new TableEntity(Constants.UsersPartitionKey, userId.ToString()))
        {
        }

        public User(TableEntity tableEntity)
            : base(tableEntity)
        {
            string serializedPlatformInfo = InternalTableEntity.GetString(_platformsKey);

            if (serializedPlatformInfo != null)
            {
                _platformUserInfo = JsonConvert.DeserializeObject<ConcurrentDictionary<string, PlatformUserInfo>>(serializedPlatformInfo);
            }
        }

        public DateTimeOffset? LastTouched { get; set; }

        /// <summary>
        /// Retrieves a collection of all <see cref="PlatformUserInfo"/> objects associated with the user.
        /// </summary>
        /// <returns>A collection of <see cref="PlatformUserInfo"/></returns>
        public IEnumerable<PlatformUserInfo> GetPlatformUserInfo()
        {
            return _platformUserInfo.ToArray().Select(info => info.Value);
        }

        /// <summary>
        /// Stores the user platform info associated with a platform.
        /// </summary>
        /// <param name="platformUserInfo">Contains the platform name associated with the user ID.</param>
        public void AddPlatformUserInfo(PlatformUserInfo platformUserInfo)
        {
            EnsureArg.IsNotNull(platformUserInfo, nameof(platformUserInfo));

            _platformUserInfo.AddOrUpdate(
                platformUserInfo.PlatformName,
                platformUserInfo,
                (key, info) => platformUserInfo != info ? platformUserInfo : info);
        }

        /// <summary>
        /// Updates the ImportState property for the specified platform.
        /// </summary>
        /// <param name="platformName">The platform to update.</param>
        /// <param name="dataImportState">The new <see cref="DataImportState"/> value.</param>
        public void UpdateImportState(string platformName, DataImportState dataImportState)
        {
            if (_platformUserInfo.TryGetValue(platformName, out var currentInfo))
            {
                currentInfo.ImportState = dataImportState;
            }
        }

        public override TableEntity ToTableEntity()
        {
            if (LastTouched != null)
            {
                InternalTableEntity.Add(nameof(LastTouched), LastTouched);
            }

            if (_platformUserInfo != null && _platformUserInfo.Count > 0)
            {
                string serializedPlatformInfo = JsonConvert.SerializeObject(_platformUserInfo);
                InternalTableEntity.Add(_platformsKey, serializedPlatformInfo);
            }

            return base.ToTableEntity();
        }
    }
}
