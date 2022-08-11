// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Azure.Data.Tables;

namespace Microsoft.Health.FitOnFhir.Common.Tests.Mocks
{
    internal class MockPageable : AsyncPageable<TableEntity>
    {
        private readonly IAsyncEnumerable<Page<TableEntity>> _enumerable;

        internal MockPageable(IList<Page<TableEntity>> pages)
        {
            _enumerable = new MockAsyncEnumerable(pages);
        }

        public override IAsyncEnumerable<Page<TableEntity>> AsPages(string continuationToken = null, int? pageSizeHint = null)
        {
            return _enumerable;
        }
    }
}
