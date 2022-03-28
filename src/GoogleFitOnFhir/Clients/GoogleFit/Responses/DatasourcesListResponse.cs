// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace GoogleFitOnFhir.Clients.GoogleFit.Responses
{
    public class DatasourcesListResponse
    {
        public IEnumerable<string> DatasourceIds { get; set; }
    }
}