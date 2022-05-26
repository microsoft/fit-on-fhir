// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Data.Tables;
using FitOnFhir.Common.Models;
using FitOnFhir.GoogleFit.Common;

namespace FitOnFhir.GoogleFit.Client.Models
{
    public class GoogleFitUser : UserBase
    {
        public GoogleFitUser(string userId)
            : base(GoogleFitConstants.GoogleFitPartitionKey, userId)
        {
        }

        public GoogleFitUser()
            : base(string.Empty, string.Empty)
        {
        }

        /// <summary>
        /// Store the last times a sync occurred (value) for each DataSource (key)
        /// </summary>
        public TableEntity LastSyncTimes => Entity;
    }
}
