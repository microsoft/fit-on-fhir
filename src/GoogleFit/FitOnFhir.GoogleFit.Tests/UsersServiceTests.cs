﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.Common.Repositories;
using FitOnFhir.Common.Tests;
using FitOnFhir.Common.Tests.Mocks;
using FitOnFhir.GoogleFit.Client.Models;
using FitOnFhir.GoogleFit.Client.Responses;
using FitOnFhir.GoogleFit.Common;
using FitOnFhir.GoogleFit.Repositories;
using FitOnFhir.GoogleFit.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Bundle = Hl7.Fhir.Model.Bundle;

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
                _logger);

            // Default responses.
            _authService.AuthTokensRequest(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Data.GetAuthTokensResponse()));
        }

        protected override Bundle PatientBundle => Data.GetBundle(Data.GetPatient());

        protected override Func<Task> ExecuteAuthorizationCallback => () => _usersService.ProcessAuthorizationCallback("TestAuthCode", CancellationToken.None);

        protected override string ExpectedPatientIdentifierSystem => Data.Issuer;

        protected override string ExpectedPatientId => Data.PatientId;

        protected override string ExpectedPlatformUserId => Data.GoogleUserId;

        protected override string ExpectedPlatform => GoogleFitConstants.GoogleFitPlatformName;

        [Fact]
        public async Task GivenAuthCodeIsNull_WhenProcessAuthorizationCallbackCalled_ConditionIsLogged()
        {
            await _usersService.ProcessAuthorizationCallback(null, CancellationToken.None);

            _logger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Information),
                Arg.Is<string>(msg => msg == "ProcessAuthorizationCallback called with no auth code"));
        }

        [Fact]
        public async Task GivenAuthCodeIsEmpty_WhenProcessAuthorizationCallbackCalled_ConditionIsLogged()
        {
            await _usersService.ProcessAuthorizationCallback(string.Empty, CancellationToken.None);

            _logger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Information),
                Arg.Is<string>(msg => msg == "ProcessAuthorizationCallback called with no auth code"));
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
