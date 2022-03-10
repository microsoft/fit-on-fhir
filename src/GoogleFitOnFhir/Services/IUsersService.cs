// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using GoogleFitOnFhir.Models;

namespace GoogleFitOnFhir.Services
{
    public interface IUsersService
    {
        Task<User> Initiate(string accessCode);

        Task ImportFitnessData(string userId);

        void QueueFitnessImport(User user);
    }
}