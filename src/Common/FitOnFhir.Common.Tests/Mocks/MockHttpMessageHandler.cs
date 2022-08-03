// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;

namespace Microsoft.Health.FitOnFhir.Common.Tests.Mocks
{
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _response;
        private readonly HttpStatusCode _statusCode;

        public MockHttpMessageHandler(string response, HttpStatusCode statusCode)
        {
            _response = response;
            _statusCode = statusCode;
        }

        public string Query { get; private set; }

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            if (request.RequestUri.Query != null)
            {
                Query = request.RequestUri.Query;
            }

            return Task.FromResult<HttpResponseMessage>(new HttpResponseMessage
            {
                StatusCode = _statusCode,
                Content = new StringContent(_response),
            });
        }
    }
}
