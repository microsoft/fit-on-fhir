// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.Common.Models;

namespace FitOnFhir.Common
{
    public class Constants
    {
        /// <summary>
        /// The name of the Table that contains a collection of <see cref="User"/>
        /// </summary>
        public const string UsersTableName = "users";

        /// <summary>
        /// Partition key for the Users partition
        /// </summary>
        public const string UsersPartitionKey = "Users";
    }
}
