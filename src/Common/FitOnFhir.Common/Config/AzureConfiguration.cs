// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace FitOnFhir.Common.Config
{
    public class AzureConfiguration
    {
        public string StorageAccountConnectionString { get; set; }

        public string UsersKeyVaultUri { get; set; }

        public string EventHubConnectionString { get; set; }
    }
}
