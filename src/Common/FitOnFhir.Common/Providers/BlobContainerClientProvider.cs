// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Health.FitOnFhir.Common.Config;

namespace Microsoft.Health.FitOnFhir.Common.Providers
{
    public class BlobContainerClientProvider : CredentialedProvider, IBlobContainerClientProvider
    {
        private readonly Uri _blobServiceUri;

        public BlobContainerClientProvider(AzureConfiguration configuration, ITokenCredentialProvider tokenCredentialProvider)
            : base(tokenCredentialProvider)
        {
            _blobServiceUri = EnsureArg.IsNotNull(configuration?.BlobServiceUri, nameof(configuration.BlobServiceUri));
        }

        public BlobContainerClient GetBlobContainerClient(string containerName)
        {
            EnsureArg.IsNotEmptyOrWhiteSpace(containerName, nameof(containerName));

            var serviceClient = new BlobServiceClient(_blobServiceUri, GetTokenCredential());

            return serviceClient.GetBlobContainerClient(containerName);
        }
    }
}
