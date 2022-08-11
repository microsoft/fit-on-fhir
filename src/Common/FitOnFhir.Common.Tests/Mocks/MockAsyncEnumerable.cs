// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Azure.Data.Tables;

namespace Microsoft.Health.FitOnFhir.Common.Tests.Mocks
{
    internal class MockAsyncEnumerable : IAsyncEnumerable<Page<TableEntity>>
    {
        private readonly IAsyncEnumerator<Page<TableEntity>> _enumerator;

        internal MockAsyncEnumerable(IList<Page<TableEntity>> pages)
        {
            _enumerator = new MockAsyncEnumerator(pages);
        }

        public IList<Page<TableEntity>> Pages { get; set; }

        public IAsyncEnumerator<Page<TableEntity>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return _enumerator;
        }
    }
}
