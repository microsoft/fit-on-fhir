// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using FitOnFhir.Common.Models;

namespace FitOnFhir.Common.Requests
{
    public class ImportRequest
    {
        public ImportRequest(QueueMessage message, CancellationToken token)
        {
            Message = EnsureArg.IsNotNull(message);
            Token = token;
        }

        public QueueMessage Message { get; set; }

        public CancellationToken Token { get; set; }
    }
}
