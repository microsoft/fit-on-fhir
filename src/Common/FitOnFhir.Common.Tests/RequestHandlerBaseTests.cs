// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Common.Handler;
using Microsoft.Health.FitOnFhir.Common.Requests;
using Xunit;

namespace Microsoft.Health.FitOnFhir.Common.Tests
{
    public abstract class RequestHandlerBaseTests<TRequest, TResult>
        where TRequest : RequestBase
        where TResult : class
    {
        public RequestHandlerBaseTests()
        {
        }

        protected IResponsibilityHandler<TRequest, TResult> RequestHandler { get; set; }

        protected abstract TRequest NonHandledRequest { get; }

        [Fact]
        public void GivenRequestRouteIsNull_WhenEvaluateCalled_NullReturned()
        {
            var result = RequestHandler.Evaluate(null);
            Assert.Null(result);
        }

        [Fact]
        public void GivenRequestRouteIsNotContainedInHanldedRoutes_WhenEvaluateCalled_NullReturned()
        {
            var result = RequestHandler.Evaluate(NonHandledRequest);
            Assert.Null(result);
        }
    }
}
