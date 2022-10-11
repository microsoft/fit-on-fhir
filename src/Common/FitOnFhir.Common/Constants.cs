// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.FitOnFhir.Common.Models;

namespace Microsoft.Health.FitOnFhir.Common
{
    public static class Constants
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
        public const string ImportDataQueueName = "import-data";

        /// <summary>
        /// The name of a blob container for auth data
        /// </summary>
        public const string AuthDataBlobContainerName = "authdata";

        /// <summary>
        /// Query parameter name for patient identifier in an external system.
        /// </summary>
        public const string ExternalIdQueryParameter = "external_id";

        /// <summary>
        /// Query parameter name for the system in which the patient identifier exists.
        /// </summary>
        public const string ExternalSystemQueryParameter = "external_system";

        /// <summary>
        /// Query parameter name for the URL that will be redirected to once
        /// authorization is completed successfully
        /// </summary>
        public const string RedirectUrlQueryParameter = "redirect_url";

        /// <summary>
        /// Query parameter name for the session state to be preserved
        /// </summary>
        public const string StateQueryParameter = "state";

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

        /// <summary>
        /// The amount of time that an AuthState instance can be used to authorize with, from the time it is created.
        /// </summary>
        public static readonly TimeSpan AuthStateExpiry = TimeSpan.FromMinutes(5);
    }
}
