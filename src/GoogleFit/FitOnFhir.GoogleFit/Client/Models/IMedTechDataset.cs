// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Messaging.EventHubs;
using Google.Apis.Fitness.v1.Data;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Config;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Client.Models
{
    public interface IMedTechDataset
    {
        /// <summary>
        /// Used to retrieve the next page of <see cref="Dataset"/> results, when a <see cref="Dataset"/> request would
        /// result in number of values that exceeds the value defined in <see cref="GoogleFitDataImporterConfiguration"/>.GoogleFitDatasetRequestLimit.
        /// When NextPageToken is null, there are no more results left to retrieve from the <see cref="Google.Apis.Fitness.v1.Data.DataSource"/>
        /// for the data set ID specified.
        /// </summary>
        string GetPageToken();

        /// <summary>
        /// Creates an EventData object that includes a serialized JSON representation of the IomtDataset.
        /// </summary>
        /// <param name="userId">The oid of the user to include in the EventData.</param>
        /// <returns><see cref="EventData"/></returns>
        EventData ToEventData(string userId);

        /// <summary>
        /// Gets the latest (most recent) start time from all points in the Dataset in nanosecond epoch time format.
        /// </summary>
        /// <returns><see cref="long"/></returns>
        long GetMaxEndTimeNanos();
    }
}
