// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using GoogleFitOnFhir.Models;

namespace GoogleFitOnFhir.Common
{
    public class PublishRequest
    {
        public QueueMessage Message { get; set; }

        public CancellationToken Token { get; set; }
    }
}
