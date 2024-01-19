// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using Azure.Data.Tables;
using EnsureThat;
using Newtonsoft.Json;

namespace Microsoft.Health.FitOnFhir.Common.Models
{
    public class User : EntityBase
    {
        private const string _platformsKey = "Platforms";
        private const string _lastTouchedKey = nameof(LastTouched);

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

            string serializedLastTouched = InternalTableEntity.GetString(_lastTouchedKey);
            if (serializedLastTouched != null)
            {
                LastTouched = JsonConvert.DeserializeObject<DateTimeOffset>(serializedLastTouched);
            }
        }

        public DateTimeOffset? LastTouched { get; set; }

        /// <summary>
        /// Retrieves the <see cref="DataImportState"/> for the specified platform.
        /// </summary>
        /// <param name="platformName">The name of the platform to retrieve the <see cref="DataImportState"/> for.</param>
        /// <param name="dataImportState">The param that will contain the <see cref="DataImportState"/></param>
        /// <returns>true if a value was found, false otherwise</returns>
        public bool TryGetPlatformImportState(string platformName, out DataImportState dataImportState)
        {
            if (_platformUserInfo.TryGetValue(platformName, out var currentInfo))
            {
                dataImportState = currentInfo.ImportState;
                return true;
            }

            dataImportState = default;
            return false;
        }

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
        /// <returns><see cref="bool"/>true if the platform exists in the collection and the <see cref="DataImportState"/> is updated.</returns>
        public bool UpdateImportState(string platformName, DataImportState dataImportState)
        {
            if (_platformUserInfo.TryGetValue(platformName, out var currentInfo))
            {
                currentInfo.ImportState = dataImportState;
                return true;
            }

            return false;
        }

        public override TableEntity ToTableEntity()
        {
            if (LastTouched != null)
            {
                string serializedLastTouched = JsonConvert.SerializeObject(LastTouched);
                InternalTableEntity.Add(_lastTouchedKey, serializedLastTouched);
            }

            if (_platformUserInfo != null && !_platformUserInfo.IsEmpty)
            {
                string serializedPlatformInfo = JsonConvert.SerializeObject(_platformUserInfo);
                InternalTableEntity.Add(_platformsKey, serializedPlatformInfo);
            }

            return base.ToTableEntity();
        }
    }
}
