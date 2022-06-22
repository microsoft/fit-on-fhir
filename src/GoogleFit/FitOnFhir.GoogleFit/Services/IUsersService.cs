// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.Common.Models;

namespace FitOnFhir.GoogleFit.Services
{
    public interface IUsersService
    {
        Task ProcessAuthorizationCallback(string accessCode, CancellationToken cancellationToken);

        Task QueueFitnessImport(User user, CancellationToken cancellationToken);
    }
}