// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using GoogleFitOnFhir.Clients.GoogleFit.Models;

namespace GoogleFitOnFhir.Clients.GoogleFit.Responses
{
    public class DataSourcesListResponse
    {
        public IEnumerable<DataSource> DataSources { get; set; }
    }
}