// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.FitOnFhir.Common.Config
{
    public class AzureConfiguration
    {
        public string FunctionPrincipalId { get; set; }

        public Uri BlobServiceUri { get; set; }

        public Uri TableServiceUri { get; set; }

        public Uri QueueServiceUri { get; set; }

        public Uri VaultUri { get; set; }

        public string EventHubFullyQualifiedNamespace { get; set; }

        public string EventHubName { get; set; }
    }
}
