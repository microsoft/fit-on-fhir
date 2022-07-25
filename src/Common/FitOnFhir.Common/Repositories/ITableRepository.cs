// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Azure.Data.Tables;

namespace Microsoft.Health.FitOnFhir.Common.Repositories
{
    public interface ITableRepository<T>
        where T : class
    {
        /// <summary>
        /// Retrieves all entities as <see cref="TableEntity"/> from the repository.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to cancel the process.</param>
        /// <returns>The <see cref="TableEntity"/> entities in an <see cref="AsyncPageable{T}"/> collection.</returns>
        AsyncPageable<TableEntity> GetAll(CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves a new instance of a specified entity from the repository.  Will catch any <see cref="RequestFailedException"/>
        /// that have a 404 status.
        /// </summary>
        /// <param name="id">The RowKey that identifies the entity.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to cancel the process.</param>
        /// <returns>A new instance of the entity created via <see cref="Activator"/>.CreateInstance.</returns>
        Task<T> GetById(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Inserts a new instance of an entity into the repository.
        /// </summary>
        /// <param name="entity">The entity to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to cancel the process.</param>
        /// <returns>A new instance of the entity created via <see cref="Activator"/>.CreateInstance.</returns>
        Task<T> Insert(T entity, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the entity in the repository with the entity provided.  If a conflict is reported by the <see cref="TableClient"/>,
        /// a <see cref="RequestFailedException"/> with status 412 will be caught and a caller provided conflict resolution Func
        /// method is then used to merge the provided entity with the entity that currently exists in the repository.
        /// In the case of a conflict, Update is called again recursively with the merged entity.
        /// </summary>
        /// <param name="entity">The entity with new values to update the existing entity in the repository with.</param>
        /// <param name="conflictResolver">A Func that the Update method uses to resolve entity conflicts (via merging) with.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to cancel the process.</param>
        /// <returns>A new instance of the entity created via <see cref="Activator"/>.CreateInstance, once the conflict has been resolved.</returns>
        Task<T> Update(T entity, Func<T, T, T> conflictResolver, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the entity in the repository with the entity provided, if one exists, otherwise adds a new instance.
        /// If a conflict is reported by the <see cref="TableClient"/>, a <see cref="RequestFailedException"/> with status
        /// 412 will be caught and a caller provided conflict resolution Func method is then used to merge the provided
        /// entity with the entity that currently exists in the repository. In the case of a conflict, Update is called again
        /// recursively with the merged entity.
        /// </summary>
        /// <param name="entity">The entity with new values to update the existing entity in the repository with, or add to the repository.</param>
        /// <param name="conflictResolver">A Func that the Update method uses to resolve entity conflicts (via merging) with.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to cancel the process.</param>
        /// <returns>A new instance of the entity created via <see cref="Activator"/>.CreateInstance, once the conflict has been resolved.</returns>
        Task<T> Upsert(T entity, Func<T, T, T> conflictResolver, CancellationToken cancellationToken);

        /// <summary>
        /// Removes the entity from the repository.
        /// </summary>
        /// <param name="entity">The entity to delete.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to cancel the process.</param>
        /// <returns>A <see cref="Task"/> representing the operation.</returns>
        Task Delete(T entity, CancellationToken cancellationToken);
    }
}
