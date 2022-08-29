// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.FitOnFhir.Common.Models;

namespace Microsoft.Health.FitOnFhir.Common.Requests
{
    public class ImportRequest : RequestBase
    {
        public ImportRequest(QueueMessage message, CancellationToken token)
        {
            Message = EnsureArg.IsNotNull(message);
            Token = token;
        }

        public QueueMessage Message { get; set; }

        public CancellationToken Token { get; set; }

        public override string Route => Message.PlatformName;
    }
}
