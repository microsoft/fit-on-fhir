// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.FitOnFhir.GoogleFit.Client.Models;
using Microsoft.Health.FitOnFhir.GoogleFit.Resolvers;
using Xunit;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Tests
{
    public class GoogleFitUserConflictResolversTests
    {
        private Func<GoogleFitUser, GoogleFitUser, GoogleFitUser> _googleFitUserConflictResolverFunc;

        public GoogleFitUserConflictResolversTests()
        {
        }

        protected GoogleFitUser NewUser { get; set; }

        protected GoogleFitUser StoredUser { get; set; }

        protected string DataStreamId => "dataStreamId";

        protected long LatestSyncTimeNanos => long.MaxValue;

        protected string PlatformUserId => "platformUserId";

        [Theory]
        [InlineData(long.MaxValue, long.MinValue)]
        [InlineData(long.MinValue, long.MaxValue)]
        public void GivenOneUserHasLatestSyncTime_WhenResolveConflictLastSyncTimesIsCalled_MergedUserLastSyncTimeIsLatest(long newSyncTime, long storedSyncTime)
        {
            CreateUsers(newSyncTime, storedSyncTime);

            _googleFitUserConflictResolverFunc = GoogleFitUserConflictResolvers.ResolveConflictLastSyncTimes;
            var mergedUser = _googleFitUserConflictResolverFunc(NewUser, StoredUser);

            mergedUser.TryGetLastSyncTime(DataStreamId, out var lastSyncTimeNanos);
            Assert.Equal(LatestSyncTimeNanos, lastSyncTimeNanos);
        }

        private void CreateUsers(long newSyncTime, long storedSyncTime)
        {
            NewUser = new GoogleFitUser(PlatformUserId);
            NewUser.SaveLastSyncTime(DataStreamId, newSyncTime);

            StoredUser = new GoogleFitUser(PlatformUserId);
            StoredUser.SaveLastSyncTime(DataStreamId, storedSyncTime);
        }
    }
}
