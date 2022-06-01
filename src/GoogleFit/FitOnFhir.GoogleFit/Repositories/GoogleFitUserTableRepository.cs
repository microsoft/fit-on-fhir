// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.Common.Persistence;
using FitOnFhir.Common.Repositories;
using FitOnFhir.GoogleFit.Client.Models;
using FitOnFhir.GoogleFit.Common;
using Microsoft.Extensions.Logging;

namespace FitOnFhir.GoogleFit.Repositories
{
    public class GoogleFitUserTableRepository : TableRepository<GoogleFitUser>, IGoogleFitUserTableRepository
    {
        public GoogleFitUserTableRepository(StorageAccountContext storageAccountContext, ILogger<GoogleFitUserTableRepository> logger)
            : base(storageAccountContext, logger)
        {
            PartitionKey = GoogleFitConstants.GoogleFitPartitionKey;
        }
    }
}
