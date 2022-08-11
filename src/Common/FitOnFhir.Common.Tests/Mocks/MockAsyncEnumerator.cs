// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Azure.Data.Tables;

namespace Microsoft.Health.FitOnFhir.Common.Tests.Mocks
{
    internal class MockAsyncEnumerator : IAsyncEnumerator<Page<TableEntity>>
    {
        private bool _hasIterated;
        private readonly IList<Page<TableEntity>> _pages;

        public MockAsyncEnumerator(IList<Page<TableEntity>> pages)
        {
            _pages = pages ?? new List<Page<TableEntity>>();
        }

        public Page<TableEntity> Current => _pages.FirstOrDefault();

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            if (_hasIterated && _pages.Count > 0)
            {
                _pages.RemoveAt(0);
            }

            _hasIterated = true;

            return _pages.Count == 0 ? ValueTask.FromResult(false) : ValueTask.FromResult(true);
        }
    }
}
