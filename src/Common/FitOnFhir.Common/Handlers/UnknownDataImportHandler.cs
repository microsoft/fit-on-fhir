// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Common.Handler;
using Microsoft.Health.FitOnFhir.Common.Requests;

namespace Microsoft.Health.FitOnFhir.Common.Handlers
{
    public class UnknownDataImportHandler : IResponsibilityHandler<ImportRequest, Task<bool?>>
    {
        public Task<bool?> Evaluate(ImportRequest request)
        {
            return Task.FromResult<bool?>(true);
        }
    }
}
