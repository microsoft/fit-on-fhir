// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Health.FitOnFhir.Common.Providers;
using Microsoft.Health.FitOnFhir.Common.Repositories;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Models;
using Microsoft.Health.FitOnFhir.GoogleFit.Common;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Repositories
{
    public class GoogleFitUserTableRepository : TableRepository<GoogleFitUser>, IGoogleFitUserTableRepository
    {
        public GoogleFitUserTableRepository(ITableClientProvider tableClientProvider, ILogger<GoogleFitUserTableRepository> logger)
            : base(tableClientProvider, logger)
        {
            PartitionKey = GoogleFitConstants.GoogleFitPartitionKey;
        }
    }
}
