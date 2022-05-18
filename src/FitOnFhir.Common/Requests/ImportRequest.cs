// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.Common.Models;

namespace FitOnFhir.Common.Requests
{
    public class ImportRequest
    {
        public QueueMessage Message { get; set; }

        public CancellationToken Token { get; set; }
    }
}
