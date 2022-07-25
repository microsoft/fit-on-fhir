// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Microsoft.Health.FitOnFhir.Common.Config;
using Microsoft.Health.FitOnFhir.Common.Repositories;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Models;
using Microsoft.Health.FitOnFhir.GoogleFit.Common;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Repositories
{
    public class GoogleFitUserTableRepository : TableRepository<GoogleFitUser>, IGoogleFitUserTableRepository
    {
        public GoogleFitUserTableRepository(AzureConfiguration azureConfiguration, TableClient tableClient, ILogger<GoogleFitUserTableRepository> logger)
            : base(azureConfiguration.StorageAccountConnectionString, tableClient, logger)
        {
            PartitionKey = GoogleFitConstants.GoogleFitPartitionKey;
        }
    }
}
