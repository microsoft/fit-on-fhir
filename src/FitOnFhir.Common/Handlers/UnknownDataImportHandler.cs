// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.Common.Requests;

namespace FitOnFhir.Common.Handlers
{
    public class UnknownDataImportHandler : UnknownOperationHandlerBase<ImportRequest, Task>
    {
        public override Task Evaluate(ImportRequest request)
        {
            return Task.CompletedTask;
        }
    }
}
