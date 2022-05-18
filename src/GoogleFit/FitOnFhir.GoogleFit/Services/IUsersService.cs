// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using FitOnFhir.Common.Models;

namespace FitOnFhir.GoogleFit.Services
{
    public interface IUsersService
    {
        Task<User> Initiate(string accessCode, CancellationToken cancellationToken);

        void QueueFitnessImport(User user);
    }
}