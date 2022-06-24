// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.Common.Models;

namespace FitOnFhir.Common.Resolvers
{
    public static class UserConflictResolvers
    {
        public static EntityBase ResolveConflictDefault(EntityBase newEntityBase, EntityBase storedEntityBase)
        {
            DataImportState mergeDataImportState;

            var newUser = new User(newEntityBase.ToTableEntity());

            // Retrieve all of the PlatformUserInfo from the new user
            var newPlatformUserInfoCollection = newUser.GetPlatformUserInfo();

            var mergedUser = new User(storedEntityBase.ToTableEntity());

            // Retrieve all of the PlatformUserInfo from the stored user
            var mergedPlatformUserInfoCollection = mergedUser.GetPlatformUserInfo();

            foreach (var mergedPlatformUserInfo in mergedPlatformUserInfoCollection)
            {
                // Retrieve the platform user info from the new user for comparison with the stored user
                var newPlatformUserInfo = newPlatformUserInfoCollection.FirstOrDefault(o => o.PlatformName == mergedPlatformUserInfo.PlatformName);
                if (newPlatformUserInfo != default)
                {
                    // If either user has this platform set as Unauthorized, then the merged user should resolve
                    // as DataImportState.Unauthorized for this platform to prevent future data imports.
                    // Otherwise default to DataImportState.ReadyToImport, as this will ensure another data import
                    // when the ImportTimerTriggerFunction runs
                    if (mergedPlatformUserInfo.ImportState == DataImportState.Unauthorized ||
                        newPlatformUserInfo.ImportState == DataImportState.Unauthorized)
                    {
                        mergeDataImportState = DataImportState.Unauthorized;
                    }
                    else
                    {
                        mergeDataImportState = DataImportState.ReadyToImport;
                    }

                    // update the ImportState
                    mergedUser.UpdateImportState(mergedPlatformUserInfo.PlatformName, mergeDataImportState);
                }
            }

            // Set the LastTouched time stamp to whichever is most recent
            mergedUser.LastTouched = mergedUser.LastTouched > newUser.LastTouched ? mergedUser.LastTouched : newUser.LastTouched;

            return mergedUser;
        }
    }
}
