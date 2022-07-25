// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.FitOnFhir.Common.Repositories;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Models;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Repositories
{
    public interface IGoogleFitUserTableRepository : ITableRepository<GoogleFitUser>
    {
    }
}
