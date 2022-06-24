// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Azure.Data.Tables;

namespace FitOnFhir.Common.Repositories
{
    public interface ITableRepository<T>
        where T : class
    {
        AsyncPageable<TableEntity> GetAll(CancellationToken cancellationToken);

        Task<T> GetById(string id, CancellationToken cancellationToken);

        Task<T> Insert(T entity, CancellationToken cancellationToken);

        Task<T> Update(T entity, CancellationToken cancellationToken, Func<T, T, T> conflictResolver);

        Task<T> Upsert(T entity, CancellationToken cancellationToken, Func<T, T, T> conflictResolver);

        Task Delete(T entity, CancellationToken cancellationToken);
    }
}
