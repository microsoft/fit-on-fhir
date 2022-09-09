// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Models;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Resolvers
{
    public static class GoogleFitUserConflictResolvers
    {
        public static GoogleFitUser ResolveConflictLastSyncTimes(GoogleFitUser newUser, GoogleFitUser mergedUser)
        {
            EnsureArg.IsNotNull(newUser, nameof(newUser));
            EnsureArg.IsNotNull(mergedUser, nameof(mergedUser));

            var newSyncTimes = newUser.GetLastSyncTimes();

            // compare the sync times between the users, and choose the latest for each to save into mergedUser
            foreach (var newSyncTime in newSyncTimes)
            {
                mergedUser.SaveLastSyncTime(newSyncTime.Key, newSyncTime.Value);
            }

            return mergedUser;
        }
    }
}
