// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.FitOnFhir.Common.Models;
using Microsoft.Health.FitOnFhir.Common.Resolvers;
using Xunit;

namespace Microsoft.Health.FitOnFhir.Common.Tests
{
    public class UserConflictResolversTests
    {
        private Func<User, User, User> _userConflictResolverFunc;

        private readonly DateTimeOffset _now =
            new DateTimeOffset(2004, 1, 12, 0, 0, 0, new TimeSpan(-5, 0, 0));

        private readonly DateTimeOffset _oneDayBack =
            new DateTimeOffset(2004, 1, 11, 0, 0, 0, new TimeSpan(-5, 0, 0));

        public UserConflictResolversTests()
        {
        }

        protected User NewUser { get; set; }

        protected User StoredUser { get; set; }

        protected string PlatformName => "platformName";

        protected string PlatformUserId => "platformUserId";

        [Theory]
        [InlineData(DataImportState.ReadyToImport, DataImportState.Unauthorized, DataImportState.Unauthorized)]
        [InlineData(DataImportState.Unauthorized, DataImportState.ReadyToImport, DataImportState.Unauthorized)]
        [InlineData(DataImportState.Queued, DataImportState.Queued, DataImportState.ReadyToImport)]
        [InlineData(DataImportState.Queued, DataImportState.Importing, DataImportState.ReadyToImport)]
        [InlineData(DataImportState.Queued, DataImportState.ReadyToImport, DataImportState.ReadyToImport)]
        [InlineData(DataImportState.Importing, DataImportState.Queued, DataImportState.ReadyToImport)]
        [InlineData(DataImportState.Importing, DataImportState.Importing, DataImportState.ReadyToImport)]
        [InlineData(DataImportState.Importing, DataImportState.ReadyToImport, DataImportState.ReadyToImport)]
        [InlineData(DataImportState.ReadyToImport, DataImportState.Queued, DataImportState.ReadyToImport)]
        [InlineData(DataImportState.ReadyToImport, DataImportState.Importing, DataImportState.ReadyToImport)]
        [InlineData(DataImportState.ReadyToImport, DataImportState.ReadyToImport, DataImportState.ReadyToImport)]
        public void GivenNoConditions_WhenResolveConflictDefaultIsCalled_MergedUserImportStateSetAppropriately(
                DataImportState newImportState,
                DataImportState storedImportState,
                DataImportState mergedImportState)
        {
            CreateUsers(newImportState, storedImportState);
            _userConflictResolverFunc = UserConflictResolvers.ResolveConflictDefault;

            var mergedUser = _userConflictResolverFunc(NewUser, StoredUser);

            var result = mergedUser.TryGetPlatformImportState(PlatformName, out var importState);
            Assert.True(result);
            Assert.Equal(mergedImportState, importState);
        }

        [Theory]
        [InlineData(DataImportState.ReadyToImport, DataImportState.Unauthorized, DataImportState.ReadyToImport)]
        [InlineData(DataImportState.Unauthorized, DataImportState.ReadyToImport, DataImportState.Unauthorized)]
        [InlineData(DataImportState.Queued, DataImportState.Importing, DataImportState.Queued)]
        [InlineData(DataImportState.Importing, DataImportState.Queued, DataImportState.Importing)]
        public void GivenStoredUserHasNoPlatformInfo_WhenResolveConflictDefaultIsCalled_MergedUserPlatformInfoSetToNewUserPlatformInfo(
            DataImportState newImportState,
            DataImportState storedImportState,
            DataImportState mergedImportState)
        {
            CreateUsers(newImportState, storedImportState, false);
            _userConflictResolverFunc = UserConflictResolvers.ResolveConflictDefault;

            var mergedUser = _userConflictResolverFunc(NewUser, StoredUser);

            var mergedPlatformInfo = mergedUser.GetPlatformUserInfo().First();

            Assert.Equal(PlatformName, mergedPlatformInfo.PlatformName);
            Assert.Equal(PlatformUserId, mergedPlatformInfo.UserId);
            Assert.Equal(mergedImportState, mergedPlatformInfo.ImportState);
        }

        [Fact]
        public void GivenNewUserIsMostRecent_WhenResolveConflictDefaultIsCalled_LastTouchedTimeIsSetToMostRecent()
        {
            CreateUsers(_now, _oneDayBack);
            _userConflictResolverFunc = UserConflictResolvers.ResolveConflictDefault;

            var mergedUser = _userConflictResolverFunc(NewUser, StoredUser);

            Assert.Equal(_now, mergedUser.LastTouched);
        }

        [Fact]
        public void GivenStoredUserIsMostRecent_WhenResolveConflictDefaultIsCalled_LastTouchedTimeIsSetToMostRecent()
        {
            CreateUsers(_oneDayBack, _now);
            _userConflictResolverFunc = UserConflictResolvers.ResolveConflictDefault;

            var mergedUser = _userConflictResolverFunc(NewUser, StoredUser);

            Assert.Equal(_now, mergedUser.LastTouched);
        }

        [Theory]
        [InlineData(DataImportState.ReadyToImport, DataImportState.Unauthorized, DataImportState.ReadyToImport)]
        [InlineData(DataImportState.ReadyToImport, DataImportState.Queued, DataImportState.Queued)]
        [InlineData(DataImportState.ReadyToImport, DataImportState.Importing, DataImportState.Importing)]
        [InlineData(DataImportState.ReadyToImport, DataImportState.ReadyToImport, DataImportState.ReadyToImport)]
        public void GivenNoConditions_WhenResolveConflictAuthorizationIsCalled_MergedUserImportStateSetAppropriately(
                        DataImportState newImportState,
                        DataImportState storedImportState,
                        DataImportState mergedImportState)
        {
            CreateUsers(newImportState, storedImportState);
            _userConflictResolverFunc = UserConflictResolvers.ResolveConflictAuthorization;

            var mergedUser = _userConflictResolverFunc(NewUser, StoredUser);

            var result = mergedUser.TryGetPlatformImportState(PlatformName, out var importState);
            Assert.True(result);
            Assert.Equal(mergedImportState, importState);
        }

        [Theory]
        [InlineData(DataImportState.ReadyToImport, DataImportState.Unauthorized, DataImportState.ReadyToImport)]
        [InlineData(DataImportState.Unauthorized, DataImportState.ReadyToImport, DataImportState.Unauthorized)]
        [InlineData(DataImportState.Queued, DataImportState.Importing, DataImportState.Queued)]
        [InlineData(DataImportState.Importing, DataImportState.Queued, DataImportState.Importing)]
        public void GivenStoredUserHasNoPlatformInfo_WhenResolveConflictAuthorizationIsCalled_MergedUserPlatformInfoSetToNewUserPlatformInfo(
            DataImportState newImportState,
            DataImportState storedImportState,
            DataImportState mergedImportState)
        {
            CreateUsers(newImportState, storedImportState, false);
            _userConflictResolverFunc = UserConflictResolvers.ResolveConflictAuthorization;

            var mergedUser = _userConflictResolverFunc(NewUser, StoredUser);

            var mergedPlatformInfo = mergedUser.GetPlatformUserInfo().First();

            Assert.Equal(PlatformName, mergedPlatformInfo.PlatformName);
            Assert.Equal(PlatformUserId, mergedPlatformInfo.UserId);
            Assert.Equal(mergedImportState, mergedPlatformInfo.ImportState);
        }

        [Fact]
        public void GivenNewUserIsMostRecent_WhenResolveConflictAuthorizationIsCalled_LastTouchedTimeIsSetToMostRecent()
        {
            CreateUsers(_now, _oneDayBack);
            _userConflictResolverFunc = UserConflictResolvers.ResolveConflictAuthorization;

            var mergedUser = _userConflictResolverFunc(NewUser, StoredUser);

            Assert.Equal(_now, mergedUser.LastTouched);
        }

        [Fact]
        public void GivenStoredUserIsMostRecent_WhenResolveConflictAuthorizationIsCalled_LastTouchedTimeIsSetToMostRecent()
        {
            CreateUsers(_oneDayBack, _now);
            _userConflictResolverFunc = UserConflictResolvers.ResolveConflictAuthorization;

            var mergedUser = _userConflictResolverFunc(NewUser, StoredUser);

            Assert.Equal(_now, mergedUser.LastTouched);
        }

        private void CreateUsers(DataImportState newImportState, DataImportState storedImportState, bool storedPlatformInfoExists = true)
        {
            NewUser = new User(Guid.NewGuid());
            NewUser.AddPlatformUserInfo(new PlatformUserInfo(PlatformName, PlatformUserId, newImportState));

            StoredUser = new User(Guid.NewGuid());
            if (storedPlatformInfoExists)
            {
                StoredUser.AddPlatformUserInfo(new PlatformUserInfo(PlatformName, PlatformUserId, storedImportState));
            }
        }

        private void CreateUsers(DateTimeOffset newLastTouched, DateTimeOffset storedLastTouched)
        {
            NewUser = new User(Guid.NewGuid());
            NewUser.LastTouched = newLastTouched;

            StoredUser = new User(Guid.NewGuid());
            StoredUser.LastTouched = storedLastTouched;
        }
    }
}
