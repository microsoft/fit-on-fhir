﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IdentityModel.Tokens.Jwt;
using Microsoft.Health.FitOnFhir.Common;
using Microsoft.Health.FitOnFhir.Common.Models;
using Microsoft.Health.FitOnFhir.Common.Repositories;
using Microsoft.Health.FitOnFhir.Common.Resolvers;
using Microsoft.Health.FitOnFhir.Common.Tests;
using Microsoft.Health.FitOnFhir.Common.Tests.Mocks;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Models;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Responses;
using Microsoft.Health.FitOnFhir.GoogleFit.Common;
using Microsoft.Health.FitOnFhir.GoogleFit.Repositories;
using Microsoft.Health.FitOnFhir.GoogleFit.Services;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Tests
{
    public class UsersServiceTests : UsersServiceBaseTests
    {
        private readonly IGoogleFitUserTableRepository _googleFitUserRepository;
        private readonly IUsersKeyVaultRepository _usersKeyVaultRepository;
        private readonly IGoogleFitAuthService _authService;
        private readonly IGoogleFitTokensService _googleFitTokensService;
        private readonly JwtSecurityToken _jwtSecurityToken = new JwtSecurityToken();
        private readonly Func<DateTimeOffset> _utcNowFunc;
        private readonly MockLogger<UsersService> _logger;
        private readonly UsersService _usersService;

        private readonly DateTimeOffset _now =
            new DateTimeOffset(2004, 1, 12, 0, 0, 0, new TimeSpan(-5, 0, 0));

        public UsersServiceTests()
        {
            _googleFitUserRepository = Substitute.For<IGoogleFitUserTableRepository>();
            _usersKeyVaultRepository = Substitute.For<IUsersKeyVaultRepository>();
            _authService = Substitute.For<IGoogleFitAuthService>();
            _googleFitTokensService = Substitute.For<IGoogleFitTokensService>();
            _utcNowFunc = Substitute.For<Func<DateTimeOffset>>();
            _utcNowFunc().Returns(_now);
            _logger = Substitute.For<MockLogger<UsersService>>();

            _usersService = new UsersService(
                ResourceService,
                UsersTableRepository,
                _googleFitUserRepository,
                _usersKeyVaultRepository,
                _authService,
                QueueService,
                _googleFitTokensService,
                _utcNowFunc,
                _logger);

            // Default responses.
            _authService.AuthTokensRequest(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Data.GetAuthTokensResponse()));
        }

        protected override Func<Task> ExecuteAuthorizationCallback => () => _usersService.ProcessAuthorizationCallback("TestAuthCode", Data.AuthorizationState, CancellationToken.None);

        protected override Func<Task> ExecuteRevokeAccess => () => _usersService.RevokeAccess(new AuthState() { ExternalIdentifier = ExpectedPatientId, ExternalSystem = ExpectedExternalSystem }, CancellationToken.None);

        protected override string ExpectedPatientIdentifierSystem => Data.Issuer;

        protected override string ExpectedPatientId => Data.PatientId;

        protected override string ExpectedPlatformUserId => Data.GoogleUserId;

        protected override string ExpectedPlatform => GoogleFitConstants.GoogleFitPlatformName;

        protected override string ExpectedExternalPatientId => Data.ExternalPatientId;

        protected override string ExpectedExternalSystem => Data.ExternalSystem;

        protected override string ExpectedAccessToken => Data.AccessToken;

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
        [InlineData(Constants.ExternalIdQueryParameter, null, Constants.ExternalSystemQueryParameter, Data.ExternalSystem)]
        [InlineData(Constants.ExternalIdQueryParameter, "", Constants.ExternalSystemQueryParameter, Data.ExternalSystem)]
        [InlineData(Constants.ExternalIdQueryParameter, Data.ExternalPatientId, Constants.ExternalSystemQueryParameter, null)]
        [InlineData(Constants.ExternalIdQueryParameter, Data.ExternalPatientId, Constants.ExternalSystemQueryParameter, "")]
        [InlineData("IncorrectPatientIdKey", Data.ExternalPatientId, Constants.ExternalSystemQueryParameter, Data.ExternalSystem)]
        [InlineData(Constants.ExternalIdQueryParameter, Data.ExternalPatientId, "IncorrectSystemKey", Data.ExternalSystem)]
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
        public async Task GivenGoogleFitUserExists_WhenProcessAuthorizationCallbackCalled_GoogleFitUserNotPersisted()
        {
            _googleFitUserRepository.GetById(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(
                x =>
                {
                    var user = new GoogleFitUser("me");
                    return user;
                });

            await ExecuteAuthorizationCallback();

            await _googleFitUserRepository.DidNotReceive().Insert(Arg.Is<GoogleFitUser>(x => x.Id.Equals(Data.GoogleUserId)), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GivenGoogleFitUserDoesNotExist_WhenProcessAuthorizationCallbackCalled_GoogleFitUserPersisted()
        {
            _googleFitUserRepository.GetById(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(
                x =>
                {
                    GoogleFitUser user = null;
                    return user;
                });

            await ExecuteAuthorizationCallback();

            await _googleFitUserRepository.Received(1).Insert(Arg.Is<GoogleFitUser>(x => x.Id.Equals(Data.GoogleUserId)), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GivenAllConditionsMet_WhenProcessAuthorizationCallbackCalled_RefreshTokenPersisted()
        {
            await ExecuteAuthorizationCallback();

            await _usersKeyVaultRepository.Received(1).Upsert(Data.GoogleUserId, Data.RefreshToken, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GivenUserForPatientDoesNotExist_WhenRevokeAccessCalled_ReturnsWithoutUpdatingUser()
        {
            UsersTableRepository.GetById(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<User>(null));

            await ExecuteRevokeAccess();

            await _googleFitTokensService.DidNotReceive().RefreshToken(Arg.Any<string>(), Arg.Any<CancellationToken>());

            await _authService.DidNotReceive().RevokeTokenRequest(Arg.Any<string>(), Arg.Any<CancellationToken>());

            await UsersTableRepository.DidNotReceive()
                .Update(Arg.Any<User>(), Arg.Any<Func<User, User, User>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GivenUserForPatientExistsWithNoMatchingPlatformInfo_WhenRevokeAccessCalled_DoesNotUpdateUser()
        {
            User testUser = GetUser(ExpectedPatientId, null, Array.Empty<(string name, DataImportState state)>());
            UsersTableRepository.GetById(ExpectedPatientId, Arg.Any<CancellationToken>()).Returns(testUser);

            await ExecuteRevokeAccess();

            await _googleFitTokensService.DidNotReceive().RefreshToken(Arg.Any<string>(), Arg.Any<CancellationToken>());

            await _authService.DidNotReceive().RevokeTokenRequest(Arg.Any<string>(), Arg.Any<CancellationToken>());

            await UsersTableRepository.DidNotReceive()
                .Update(Arg.Any<User>(), Arg.Any<Func<User, User, User>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GivenUserForPatientExists_WhenRevokeAccessCalled_UpdatesUser()
        {
            User testUser = GetUser(ExpectedPatientId, ExpectedPlatformUserId, (ExpectedPlatform, DataImportState.ReadyToImport));
            UsersTableRepository.GetById(ExpectedPatientId, Arg.Any<CancellationToken>()).Returns(testUser);

            AuthTokensResponse tokensResponse = new AuthTokensResponse() { AccessToken = Data.AccessToken, IdToken = _jwtSecurityToken, RefreshToken = Data.RefreshToken };
            _googleFitTokensService.RefreshToken(
                Arg.Is<string>(str => str == ExpectedPlatformUserId),
                Arg.Any<CancellationToken>()).Returns(tokensResponse);

            await ExecuteRevokeAccess();

            await UsersTableRepository.Received(1)
                .Update(
                    Arg.Is<User>(usr => usr == testUser),
                    Arg.Is<Func<User, User, User>>(f => f == UserConflictResolvers.ResolveConflictAuthorization),
                    Arg.Any<CancellationToken>());
        }
    }
}
