// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using GoogleFitOnFhir.Clients.GoogleFit;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;
using GoogleFitOnFhir.Repositories;
using GoogleFitOnFhir.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace GoogleFitOnFhir.UnitTests
{
    public class GoogleFitUserServiceTests
    {
        private readonly IUsersService _usersService;

        private readonly IUsersTableRepository _usersTableRepository;
        private readonly IGoogleFitClient _googleFitClient;
        private readonly ILogger<UsersService> _logger;
        private readonly IUsersKeyVaultRepository _usersKeyvaultRepository;
        private readonly IGoogleFitAuthService _authService;

        public GoogleFitUserServiceTests()
        {
            _usersKeyvaultRepository = Substitute.For<IUsersKeyVaultRepository>();
            _googleFitClient = Substitute.For<IGoogleFitClient>();
            _authService = Substitute.For<IGoogleFitAuthService>();
            _usersTableRepository = Substitute.For<IUsersTableRepository>();
            _logger = Substitute.For<ILogger<UsersService>>();

            _usersService = new UsersService(_usersTableRepository, _googleFitClient, _usersKeyvaultRepository, _authService, _logger);
        }

        [Fact]
        public void GivenRequestHandledAndExceptionIsThrown_WhenAccessCodeIsNotValid_ReturnsExceptionTokenResponseEmpty()
        {
            Task<Exception> exception = Assert.ThrowsAsync<Exception>(() => _usersService.Initiate(Arg.Any<string>(), Arg.Any<CancellationToken>()));
            Assert.Equal(exception.Result.Message, new Exception("Token response empty").Message);
        }

        [Fact]
        public void GivenRequestHandledAndExceptionIsThrown_WhenEmailResponseIsEmpty_ReturnsExceptionTokenResponseEmpty()
        {
            _authService.AuthTokensRequest(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new AuthTokensResponse());
            _googleFitClient.MyEmailRequest(Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new Exception("Email response empty"));

            Task<Exception> exception = Assert.ThrowsAsync<Exception>(() => _usersService.Initiate(Arg.Any<string>(), Arg.Any<CancellationToken>()));
            Assert.Equal(exception.Result.Message, new Exception("Email response empty").Message);
        }
    }
}
