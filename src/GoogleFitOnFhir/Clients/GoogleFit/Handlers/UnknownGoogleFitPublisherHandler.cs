// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using GoogleFitOnFhir.Common;

namespace GoogleFitOnFhir.Clients.GoogleFit.Handlers
{
    public class UnknownGoogleFitPublisherHandler : UnknownOperationHandlerBase<PublishRequest, Task>
    {
        public override Task Evaluate(PublishRequest request)
        {
            return Task.CompletedTask;
        }
    }
}
