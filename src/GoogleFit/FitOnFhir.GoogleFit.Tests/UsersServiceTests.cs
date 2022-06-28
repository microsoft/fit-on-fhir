// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.Common;
using FitOnFhir.Common.Repositories;
using FitOnFhir.Common.Tests;
using FitOnFhir.Common.Tests.Mocks;
using FitOnFhir.GoogleFit.Client.Models;
using FitOnFhir.GoogleFit.Client.Responses;
using FitOnFhir.GoogleFit.Common;
using FitOnFhir.GoogleFit.Repositories;
using FitOnFhir.GoogleFit.Services;
using NSubstitute;
using Xunit;

namespace FitOnFhir.GoogleFit.Tests
{
    public class UsersServiceTests : UsersServiceBaseTests
    {
        private readonly IGoogleFitUserTableRepository _googleFitUserRepository;
        private readonly IUsersKeyVaultRepository _usersKeyVaultRepository;
        private readonly IGoogleFitAuthService _authService;
        private readonly MockLogger<UsersService> _logger;
        private readonly UsersService _usersService;

        public UsersServiceTests()
        {
            _googleFitUserRepository = Substitute.For<IGoogleFitUserTableRepository>();
            _usersKeyVaultRepository = Substitute.For<IUsersKeyVaultRepository>();
            _authService = Substitute.For<IGoogleFitAuthService>();
            _logger = Substitute.For<MockLogger<UsersService>>();

            _usersService = new UsersService(
                ResourceService,
                UsersTableRepository,
                _googleFitUserRepository,
                _usersKeyVaultRepository,
                _authService,
                QueueService,
                _logger);

            // Default responses.
            _authService.AuthTokensRequest(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Data.GetAuthTokensResponse()));
        }

        protected override Func<Task> ExecuteAuthorizationCallback => () => _usersService.ProcessAuthorizationCallback("TestAuthCode", Data.AuthorizationState, CancellationToken.None);

        protected override string ExpectedPatientIdentifierSystem => Data.Issuer;

        protected override string ExpectedPatientId => Data.PatientId;

        protected override string ExpectedPlatformUserId => Data.GoogleUserId;

        protected override string ExpectedPlatform => GoogleFitConstants.GoogleFitPlatformName;

        protected override string ExpectedExternalPatientId => Data.ExternalPatientId;

        protected override string ExpectedExternalSystem => Data.ExternalSystem;

        [Fact]
        public async Task GivenAuthCodeIsNull_WhenProcessAuthorizationCallbackCalled_ExceptionIsThrown()
        {
            await Assert.ThrowsAsync<Exception>(() => _usersService.ProcessAuthorizationCallback(null, Data.AuthorizationState, CancellationToken.None));
        }

        [Fact]
        public async Task GivenAuthCodeIsEmpty_WhenProcessAuthorizationCallbackCalled_ExceptionIsThrown()
        {
            await Assert.ThrowsAsync<Exception>(() => _usersService.ProcessAuthorizationCallback(string.Empty, Data.AuthorizationState, CancellationToken.None));
        }

        [Fact]
        public async Task GivenStateIsNull_WhenProcessAuthorizationCallbackCalled_ExceptionIsThrown()
        {
            await Assert.ThrowsAsync<Exception>(() => _usersService.ProcessAuthorizationCallback("TestAuthCode", null, CancellationToken.None));
        }

        [Fact]
        public async Task GivenStateIsEmpty_WhenProcessAuthorizationCallbackCalled_ExceptionIsThrown()
        {
            await Assert.ThrowsAsync<Exception>(() => _usersService.ProcessAuthorizationCallback("TestAuthCode", string.Empty, CancellationToken.None));
        }

        [Theory]
        [InlineData(Constants.PatientIdQueryParameter, null, Constants.SystemQueryParameter, Data.ExternalSystem)]
        [InlineData(Constants.PatientIdQueryParameter, "", Constants.SystemQueryParameter, Data.ExternalSystem)]
        [InlineData(Constants.PatientIdQueryParameter, Data.ExternalPatientId, Constants.SystemQueryParameter, null)]
        [InlineData(Constants.PatientIdQueryParameter, Data.ExternalPatientId, Constants.SystemQueryParameter, "")]
        [InlineData("IncorrectPatientIdKey", Data.ExternalPatientId, Constants.SystemQueryParameter, Data.ExternalSystem)]
        [InlineData(Constants.PatientIdQueryParameter, Data.ExternalPatientId, "IncorrectSystemKey", Data.ExternalSystem)]
        public async Task GivenStateIsFormattedIncorrectly_WhenProcessAuthorizationCallbackCalled_ExceptionIsThrown(string patientKey, string patientValue, string systemKey, string systemValue)
        {
            string patientId = patientValue == null ? "null" : $"\"{patientValue}\"";
            string system = systemValue == null ? "null" : $"\"{systemValue}\"";
            string state = $"{{\"{patientKey}\":{patientId}, \"{systemKey}\":{system}}}";

            await Assert.ThrowsAsync<Exception>(() => _usersService.ProcessAuthorizationCallback("TestAuthCode", state, CancellationToken.None));
        }

        [Fact]
        public async Task GivenAuthServiceAuthTokensRequestReturnsNull_WhenProcessAuthorizationCallbackCalled_ExceptionIsThrown()
        {
            _authService.AuthTokensRequest(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<AuthTokensResponse>(null));

            await Assert.ThrowsAsync<Exception>(async () => await ExecuteAuthorizationCallback());
        }

        [Fact]
        public async Task GivenAllConditionsMet_WhenProcessAuthorizationCallbackCalled_GoogleFitUserPersisted()
        {
            await ExecuteAuthorizationCallback();

            await _googleFitUserRepository.Received(1).Upsert(Arg.Is<GoogleFitUser>(x => x.Id.Equals(Data.GoogleUserId)), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GivenAllConditionsMet_WhenProcessAuthorizationCallbackCalled_RefreshTokenPersisted()
        {
            await ExecuteAuthorizationCallback();

            await _usersKeyVaultRepository.Received(1).Upsert(Data.GoogleUserId, Data.RefreshToken, Arg.Any<CancellationToken>());
        }
    }
}
