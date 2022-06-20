// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.GoogleFit.Client.Models;

namespace FitOnFhir.GoogleFit.Client.Responses
{
    public class DataSourcesListResponse
    {
        public IEnumerable<DataSource> DataSources { get; set; }
    }
}