// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.FitOnFhir.Common.Models;

namespace Microsoft.Health.FitOnFhir.Common.Resolvers
{
    public static class UserConflictResolvers
    {
        /// <summary>
        /// A storage account entity update conflict resolver, with default behavior.  Default behavior
        /// means that if either entity being compared has a platform <see cref="DataImportState"/> set to Unauthorized, then the resulting merged
        /// entity must also have its <see cref="DataImportState"/> set to <see cref="DataImportState"/>.Unauthorized.  In addition, the most
        /// recent LastTouched property will be propagated.
        /// </summary>
        /// <param name="newUser">The <see cref="User"/> from the original Update or Upsert operation.</param>
        /// <param name="mergedUser">The <see cref="User"/> that currently exists in the storage account.</param>
        /// <returns>A merged <see cref="User"/> with the correct <see cref="DataImportState"/> for each <see cref="PlatformUserInfo"/> stored.</returns>
        public static User ResolveConflictDefault(User newUser, User mergedUser)
        {
            EnsureArg.IsNotNull(newUser, nameof(newUser));
            EnsureArg.IsNotNull(mergedUser, nameof(mergedUser));

            DataImportState mergeDataImportState;

            // Retrieve all of the PlatformUserInfo from the new user
            var newPlatformUserInfoCollection = newUser.GetPlatformUserInfo();

            // Retrieve all of the PlatformUserInfo from the stored user
            var storedPlatformUserInfoCollection = mergedUser.GetPlatformUserInfo();

            foreach (var newPlatformUserInfo in newPlatformUserInfoCollection)
            {
                // Retrieve the platform user info from the new user for comparison with the stored user
                var storedPlatformUserInfo = storedPlatformUserInfoCollection.FirstOrDefault(o => o.PlatformName == newPlatformUserInfo.PlatformName);
                if (storedPlatformUserInfo != default)
                {
                    // If either user has this platform set as Unauthorized, then the merged user should resolve
                    // as DataImportState.Unauthorized for this platform to prevent future data imports.
                    // Otherwise default to DataImportState.ReadyToImport, as this will ensure another data import
                    // when the ImportTimerTriggerFunction runs
                    if (storedPlatformUserInfo.ImportState == DataImportState.Unauthorized ||
                        newPlatformUserInfo.ImportState == DataImportState.Unauthorized)
                    {
                        mergeDataImportState = DataImportState.Unauthorized;
                    }
                    else
                    {
                        mergeDataImportState = DataImportState.ReadyToImport;
                    }

                    // update the ImportState
                    mergedUser.UpdateImportState(storedPlatformUserInfo.PlatformName, mergeDataImportState);
                } // this platform is new to the stored user, so add it in
                else
                {
                    mergedUser.AddPlatformUserInfo(newPlatformUserInfo);
                }
            }

            // Set the LastTouched time stamp to whichever is most recent
            mergedUser.LastTouched = mergedUser.LastTouched > newUser.LastTouched ? mergedUser.LastTouched : newUser.LastTouched;

            return mergedUser;
        }

        /// <summary>
        /// A storage account entity update conflict resolver, which is meant to cover authorization scenarios, where a user
        /// previously revoked access and now wishes to grant access again.  The <see cref="DataImportState"/> for that platform will be
        /// set to <see cref="DataImportState"/>.ReadyToImport.  In addition, the most recent LastTouched property will be propagated.
        /// </summary>
        /// <param name="newUser">The <see cref="User"/> from the original Update or Upsert operation.</param>
        /// <param name="mergedUser">The <see cref="User"/> that currently exists in the storage account.</param>
        /// <returns>A merged <see cref="User"/> with the correct <see cref="DataImportState"/> for each <see cref="PlatformUserInfo"/> stored.</returns>
        public static User ResolveConflictAuthorization(User newUser, User mergedUser)
        {
            EnsureArg.IsNotNull(newUser, nameof(newUser));
            EnsureArg.IsNotNull(mergedUser, nameof(mergedUser));

            // Retrieve all of the PlatformUserInfo from the new user
            var newPlatformUserInfoCollection = newUser.GetPlatformUserInfo();

            // Retrieve all of the PlatformUserInfo from the new user
            var storedPlatformUserInfoCollection = mergedUser.GetPlatformUserInfo();

            foreach (var newPlatformUserInfo in newPlatformUserInfoCollection)
            {
                // Retrieve the platform user info from the new user for comparison with the stored user
                var storedPlatformUserInfo = storedPlatformUserInfoCollection.FirstOrDefault(o => o.PlatformName == newPlatformUserInfo.PlatformName);
                if (storedPlatformUserInfo != default)
                {
                    if (newPlatformUserInfo.ImportState == DataImportState.ReadyToImport &&
                        storedPlatformUserInfo.ImportState == DataImportState.Unauthorized)
                    {
                        // set the ImportState to ReadyToImport to allow for reauthorization
                        mergedUser.UpdateImportState(newPlatformUserInfo.PlatformName, DataImportState.ReadyToImport);
                    }
                } // this platform is new to the stored user, so add it in
                else
                {
                    mergedUser.AddPlatformUserInfo(newPlatformUserInfo);
                }
            }

            // Set the LastTouched time stamp to whichever is most recent
            mergedUser.LastTouched = mergedUser.LastTouched > newUser.LastTouched ? mergedUser.LastTouched : newUser.LastTouched;

            return mergedUser;
        }
    }
}
