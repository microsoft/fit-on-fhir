// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using FitOnFhir.GoogleFit.Clients.GoogleFit.Models;

namespace FitOnFhir.GoogleFit.Clients.GoogleFit.Responses
{
    public class DataSourcesListResponse
    {
        public IEnumerable<DataSource> DataSources { get; set; }
    }
}