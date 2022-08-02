// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.FitOnFhir.Common.Models;

namespace Microsoft.Health.FitOnFhir.Common
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

        /// <summary>
        /// The name of the Azure storage queue
        /// </summary>
        public const string QueueName = "import-data";

        /// <summary>
        /// Query parameter name for patient identifier in an external system.
        /// </summary>
        public const string ExternalIdQueryParameter = "externalId";

        /// <summary>
        /// Query parameter name for the system in which the patient identifier exists.
        /// </summary>
        public const string ExternalSystemQueryParameter = "externalSystem";

        /// <summary>
        /// The name of the container in blob storage that holds the temporary authorization credentials.
        /// </summary>
        public const string BlobStorageContainerName = "FitOnFhirAuthStorage";

        /// <summary>
        /// The name of the blob containing the authorization credentials.
        /// </summary>
        public const string BlobName = "FitOnFhirAuthBlob";

        /// <summary>
        /// The length of a generated nonce.
        /// </summary>
        public const int NonceLength = 24;
    }
}
