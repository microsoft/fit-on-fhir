// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace GoogleFitOnFhir.Repositories
{
    public interface IGenericRepository<T>
    where T : class
    {
        IEnumerable<T> GetAll();

        T GetById(string id);

        void Insert(T obj);

        void Update(T obj);

        void Upsert(T obj);

        void Delete(T obj);
    }
}