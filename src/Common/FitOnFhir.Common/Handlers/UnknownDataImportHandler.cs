﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.Common.Requests;

namespace FitOnFhir.Common.Handlers
{
    public class UnknownDataImportHandler : UnknownOperationHandlerBase<ImportRequest, Task<bool?>>
    {
        public override Task<bool?> Evaluate(ImportRequest request)
        {
            return Task.FromResult<bool?>(true);
        }
    }
}
