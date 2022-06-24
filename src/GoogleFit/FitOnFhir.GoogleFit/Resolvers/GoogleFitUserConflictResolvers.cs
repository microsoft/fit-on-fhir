// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.Common.Models;
using FitOnFhir.GoogleFit.Client.Models;

namespace FitOnFhir.GoogleFit.Resolvers
{
    public static class GoogleFitUserConflictResolvers
    {
        public static EntityBase ResolveConflictLastSyncTimes(EntityBase newEntityBase, EntityBase storedEntityBase)
        {
            var mergedUser = new GoogleFitUser(storedEntityBase.ToTableEntity());
            var newUser = new GoogleFitUser(newEntityBase.ToTableEntity());

            var newSynctimes = newUser.GetLastSyncTimes();

            // compare the sync times between the users, and choose the latest for each to save into mergedUser
            foreach (var newSynctime in newSynctimes)
            {
                mergedUser.SaveLastSyncTime(newSynctime.Key, newSynctime.Value);
            }

            return mergedUser;
        }
    }
}
