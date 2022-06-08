// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.Common.Config;

namespace FitOnFhir.Common.Persistence
{
    public class EventHubContext : ConnectionStringContext
    {
        public EventHubContext(AzureConfiguration azureConfiguration)
        {
            ConnectionString = azureConfiguration.AzureWebJobsStorage;
        }
    }
}