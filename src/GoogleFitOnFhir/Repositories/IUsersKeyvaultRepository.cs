// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;

namespace GoogleFitOnFhir.Repositories
{
    public interface IUsersKeyvaultRepository
    {
        Task Upsert(string secretName, string value);

        Task<string> GetByName(string secretName);
    }
}