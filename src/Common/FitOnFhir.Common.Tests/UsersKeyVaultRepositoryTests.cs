// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Health.FitOnFhir.Common.Providers;
using Microsoft.Health.FitOnFhir.Common.Repositories;
using Microsoft.Health.FitOnFhir.Common.Tests.Mocks;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.FitOnFhir.Common.Tests
{
    public class UsersKeyVaultRepositoryTests
    {
        private readonly MockSecretClient _secretClient;
        private readonly UsersKeyVaultRepository _repository;

        public UsersKeyVaultRepositoryTests()
        {
            _secretClient = Substitute.For<MockSecretClient>();
            ISecretClientProvider provider = Substitute.For<ISecretClientProvider>();
            provider.GetSecretClient().Returns(_secretClient);
            _repository = new UsersKeyVaultRepository(provider);
        }

        [Theory]
        [InlineData("TestName", null, typeof(ArgumentNullException))]
        [InlineData("TestName", "", typeof(ArgumentException))]
        [InlineData(null, "TestValue", typeof(ArgumentNullException))]
        [InlineData("", "TestValue", typeof(ArgumentException))]
        public async Task GivenRequiredParametersAreNullOrEmpty_WhenUpsertCalled_ExceptionThrown(string name, string value, Type exceptionType)
        {
            await Assert.ThrowsAsync(exceptionType, () => _repository.Upsert(name, value, CancellationToken.None));
        }

        [Theory]
        [InlineData(null, typeof(ArgumentNullException))]
        [InlineData("", typeof(ArgumentException))]
        public async Task GivenRequiredParametersAreNullOrEmpty_WhenGetByNameCalled_ExceptionThrown(string name, Type exceptionType)
        {
            await Assert.ThrowsAsync(exceptionType, () => _repository.GetByName(name, CancellationToken.None));
        }

        [Fact]
        public async Task GivenStartRecoverDeletedSecretAsyncThrows_WhenUpsertCalled_ExceptionThrown()
        {
            _secretClient.StartRecoverDeletedSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns<Task<RecoverDeletedSecretOperation>>(x => throw new ArgumentException());

            await Assert.ThrowsAsync<ArgumentException>(() => _repository.Upsert("TestSecretName", "TestSecretValue", CancellationToken.None));
        }

        [Fact]
        public async Task GivenSetSecretAsyncThrows_WhenUpsertCalled_ExceptionThrown()
        {
            _secretClient.GetDeletedSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns<Response<DeletedSecret>>(x => throw new RequestFailedException(404, "Not Found"));
            _secretClient.SetSecretAsync(Arg.Any<KeyVaultSecret>(), Arg.Any<CancellationToken>()).Returns<Response<KeyVaultSecret>>(x => throw new ArgumentNullException());

            await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.Upsert("TestSecretName", "TestSecretValue", CancellationToken.None));
        }

        [Fact]
        public async Task GivenGetSecretAsyncThrows_WhenGetByNameCalled_ExceptionThrown()
        {
            _secretClient.GetSecretAsync(Arg.Any<string>(), null, Arg.Any<CancellationToken>()).Returns<Response<KeyVaultSecret>>(x => throw new ArgumentNullException());

            await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.GetByName("TestSecretName", CancellationToken.None));
        }

        [Fact]
        public async Task GivenSecretWasNotPreviouslyDeleted_WhenUpsertCalled_SetSecretCalled()
        {
            _secretClient.GetDeletedSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns<Response<DeletedSecret>>(x => throw new RequestFailedException(404, "Not Found"));

            await _repository.Upsert("TestSecretName", "TestSecretValue", CancellationToken.None);

            await _secretClient.DidNotReceive().StartRecoverDeletedSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
            await _secretClient.Received(1).SetSecretAsync(Arg.Is<KeyVaultSecret>(x => Validate(x, "TestSecretName", "TestSecretValue")), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GivenSecretWasPreviouslyDeleted_WhenUpsertCalled_SetSecretCalled()
        {
            _secretClient.GetDeletedSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new MockResponse());
            _secretClient.StartRecoverDeletedSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new MockRecoverDeletedSecretOperation());

            await _repository.Upsert("TestSecretName", "TestSecretValue", CancellationToken.None);

            await _secretClient.Received(1).StartRecoverDeletedSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
            await _secretClient.Received(1).SetSecretAsync(Arg.Is<KeyVaultSecret>(x => Validate(x, "TestSecretName", "TestSecretValue")), Arg.Any<CancellationToken>());
        }

        private bool Validate(KeyVaultSecret secret, string expectedName, string expectedValue)
        {
            return string.Equals(expectedName, secret.Name, StringComparison.Ordinal) &&
                string.Equals(expectedValue, secret.Value, StringComparison.Ordinal);
        }
    }
}
