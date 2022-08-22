// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Common.Handler;

namespace Microsoft.Health.FitOnFhir.Common.Handlers
{
    public abstract class OperationHandlerBase<TRequest, TResult> : IResponsibilityHandler<TRequest, TResult>
    where TResult : class
    {
        public abstract IEnumerable<string> HandledRoutes { get; }

        public virtual TResult Evaluate(TRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
