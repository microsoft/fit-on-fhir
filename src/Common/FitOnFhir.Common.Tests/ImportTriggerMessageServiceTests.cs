// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.FitOnFhir.Common.Models;
using Microsoft.Health.FitOnFhir.Common.Repositories;
using Microsoft.Health.FitOnFhir.Common.Services;
using Microsoft.Health.FitOnFhir.Common.Tests.Mocks;
using Newtonsoft.Json;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.FitOnFhir.Common.Tests
{
    public class ImportTriggerMessageServiceTests
    {
        private readonly IUsersTableRepository _usersTableRepository;
        private readonly ICollector<string> _collector;
        private readonly ImportTriggerMessageService _service;

        public ImportTriggerMessageServiceTests()
        {
            _usersTableRepository = Substitute.For<IUsersTableRepository>();
            _usersTableRepository.Update(Arg.Any<User>(), Arg.Any<Func<User, User, User>>(), Arg.Any<CancellationToken>()).Returns(x => x.ArgAt<User>(0));
            _collector = Substitute.For<ICollector<string>>();
            _service = new ImportTriggerMessageService(_usersTableRepository, NullLogger<ImportTriggerMessageService>.Instance);
        }

        [Fact]
        public async Task GivenNoUsers_WhenAddImportMessagesToCollectorCalled_NoMessagesAdded()
        {
            _usersTableRepository.GetAll(Arg.Any<CancellationToken>()).Returns(GetTableRepositoryResponse(new List<PlatformUserInfo>()));

            await _service.AddImportMessagesToCollector(_collector, CancellationToken.None);

            _collector.DidNotReceive().Add(Arg.Any<string>());
        }

        [Theory]
        [InlineData(DataImportState.Importing)]
        [InlineData(DataImportState.Unauthorized)]
        [InlineData(DataImportState.Queued)]
        public async Task GivenUserDataImportStateNotReady_WhenAddImportMessagesToCollectorCalled_NoMessagesAdded(DataImportState state)
        {
            PlatformUserInfo platformUserInfo = new PlatformUserInfo("TestPlatformName", "TestUserId", state);
            _usersTableRepository.GetAll(Arg.Any<CancellationToken>()).Returns(GetTableRepositoryResponse(new List<PlatformUserInfo>() { platformUserInfo }));

            await _service.AddImportMessagesToCollector(_collector, CancellationToken.None);

            _collector.DidNotReceive().Add(Arg.Any<string>());
        }

        [Fact]
        public async Task GivenUserImportStateIsReady_WhenAddImportMessagesToCollectorCalled_ImportStateUpdatedAndMessageAdded()
        {
            PlatformUserInfo platformUserInfo = new PlatformUserInfo("TestPlatformName", "TestUserId", DataImportState.ReadyToImport);
            _usersTableRepository.GetAll(Arg.Any<CancellationToken>()).Returns(GetTableRepositoryResponse(new List<PlatformUserInfo>() { platformUserInfo }));

            await _service.AddImportMessagesToCollector(_collector, CancellationToken.None);

            await _usersTableRepository.Received(1).Update(Arg.Is<User>(x => VerifyUser(x, DataImportState.Queued, "TestPlatformName")), Arg.Any<Func<User, User, User>>(), Arg.Any<CancellationToken>());
            _collector.Received(1).Add(Arg.Is<string>(x => VerifyMessage(x, "TestPlatformName", "TestUserIdRoot", "TestUserId")));
        }

        [Theory]
        [InlineData(1, 3)]
        [InlineData(3, 1)]
        public async Task GivenMultipleUsersWithImportStateIsReady_WhenAddImportMessagesToCollectorCalled_ImportStatesUpdatedAndMessagesAdded(int pageCount, int usersPerPage)
        {
            List<List<PlatformUserInfo>> pages = new List<List<PlatformUserInfo>>();

            for (int i = 0; i < pageCount; i++)
            {
                pages.Add(new List<PlatformUserInfo>());

                for (int j = 0; j < usersPerPage; j++)
                {
                    pages[i].Add(new PlatformUserInfo("TestPlatformName", $"TestUserId{i}{j}", DataImportState.ReadyToImport));
                }
            }

            _usersTableRepository.GetAll(Arg.Any<CancellationToken>()).Returns(GetTableRepositoryResponse(pages.ToArray()));

            await _service.AddImportMessagesToCollector(_collector, CancellationToken.None);

            await _usersTableRepository.Received(3).Update(Arg.Any<User>(), Arg.Any<Func<User, User, User>>(), Arg.Any<CancellationToken>());
            _collector.Received(3).Add(Arg.Any<string>());
        }

        private bool VerifyMessage(string messageString, string expectedPlatform, string expectedUserId, string expectedPlatformUserId)
        {
            QueueMessage message = JsonConvert.DeserializeObject<QueueMessage>(messageString);

            return string.Equals(expectedPlatform, message.PlatformName) &&
                string.Equals(expectedUserId, message.UserId) &&
                string.Equals(expectedPlatformUserId, message.PlatformUserId);
        }

        private bool VerifyUser(User user, DataImportState expectedState, string expectedPlatform)
        {
            if (user.TryGetPlatformImportState(expectedPlatform, out DataImportState actualState))
            {
                return expectedState == actualState;
            }

            return false;
        }

        private AsyncPageable<TableEntity> GetTableRepositoryResponse(params IList<PlatformUserInfo>[] platformUserInfoLists)
        {
            IList<Page<TableEntity>> pages = new List<Page<TableEntity>>();

            foreach (IList<PlatformUserInfo> platformUserInfoList in platformUserInfoLists)
            {
                IList<TableEntity> entities = new List<TableEntity>();

                foreach (PlatformUserInfo platformUserInfo in platformUserInfoList)
                {
                    TableEntity entity = new TableEntity();
                    entity.TryAdd("Platforms", $"{{\"{platformUserInfo.PlatformName}\":{JsonConvert.SerializeObject(platformUserInfo)}}}");
                    entity.RowKey = $"{platformUserInfo.UserId}Root";
                    entities.Add(entity);
                }

                pages.Add(Page<TableEntity>.FromValues(entities.ToArray(), null, Substitute.For<Response>()));
            }

            return new MockPageable(pages);
        }
    }
}
