// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text;
using Azure.Messaging.EventHubs;
using EnsureThat;
using FitOnFhir.GoogleFit.Common;
using Google.Apis.Fitness.v1.Data;
using Newtonsoft.Json.Linq;

namespace FitOnFhir.GoogleFit.Client.Models
{
    public class MedTechDataset
    {
        private readonly Dataset _dataset;
        private readonly DataSource _dataSource;

        public MedTechDataset(Dataset dataset, DataSource dataSource)
        {
            _dataset = EnsureArg.IsNotNull(dataset, nameof(dataset));
            _dataSource = EnsureArg.IsNotNull(dataSource, nameof(dataSource));
        }

        /// <summary>
        /// Creates an EventData object that includes a serialized JSON represention of the IomtDataset.
        /// </summary>
        /// <param name="userId">The oid of the user to include in the EventData.</param>
        /// <returns><see cref="EventData"/></returns>
        public EventData ToEventData(string userId)
        {
            EnsureArg.IsNotNullOrWhiteSpace(userId, nameof(userId));

            var json = JObject.FromObject(_dataset);
            json[GoogleFitConstants.PatientIdentifier] = userId;
            json[GoogleFitConstants.DeviceIdentifier] = GetDeviceId(userId);

            return new EventData(json.ToString());
        }

        /// <summary>
        /// Accessor to the <see cref="Dataset"/>
        /// </summary>
        /// <returns>The current Dataset value.</returns>
        public Dataset GetDataset()
        {
            return _dataset;
        }

        /// <summary>
        /// Generates a unique device identifier for the dataset.
        /// <remarks>
        /// Datasets and DataSources do not contain a globally unique identifier.
        /// This method generates a globally unique identifier by combining the user's oid,
        /// the application package name and device uid.
        /// </remarks>
        /// </summary>
        /// <param name="userId">The oid of the user.</param>
        /// <returns>a string representing a globally unique device identifier.</returns>
        /// <exception cref="InvalidOperationException">Thrown if both application package name and device uid are both null or empty.</exception>
        private string GetDeviceId(string userId)
        {
            EnsureArg.IsNotNullOrWhiteSpace(userId, nameof(userId));

            if (string.IsNullOrWhiteSpace(_dataSource.ApplicationPackageName) && string.IsNullOrWhiteSpace(_dataSource.DeviceUid))
            {
                throw new InvalidOperationException("_dataSource.ApplicationPackageName and/or _dataSource.DeviceUid are required to generate a unique device identifier.");
            }

            StringBuilder stringBuilder = new StringBuilder($"{userId}");

            if (!string.IsNullOrWhiteSpace(_dataSource.ApplicationPackageName))
            {
                stringBuilder.Append($".{_dataSource.ApplicationPackageName}");
            }

            if (!string.IsNullOrWhiteSpace(_dataSource.DeviceUid))
            {
                stringBuilder.Append($".{_dataSource.DeviceUid}");
            }

            return stringBuilder.ToString();
        }
    }
}
