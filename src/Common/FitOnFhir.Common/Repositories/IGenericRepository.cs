// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;

namespace FitOnFhir.Common.Repositories
{
    public interface IGenericRepository<T>
        where T : class
    {
        AsyncPageable<T> GetAll(CancellationToken cancellationToken);

        Task<T> GetById(string id, CancellationToken cancellationToken);

        Task Insert(T entity, CancellationToken cancellationToken);

        Task Update(T entity, CancellationToken cancellationToken);

        Task Upsert(T entity, CancellationToken cancellationToken);

        Task Delete(T entity, CancellationToken cancellationToken);
    }
}