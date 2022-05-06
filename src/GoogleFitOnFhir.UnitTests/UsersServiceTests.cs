// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GoogleFitOnFhir.Clients.GoogleFit;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;
using GoogleFitOnFhir.Models;
using GoogleFitOnFhir.Repositories;
using GoogleFitOnFhir.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Xunit;

namespace GoogleFitOnFhir.UnitTests
{
    public class UsersServiceTests
    {
        private readonly IUsersService _usersService;

        private readonly IUsersTableRepository _usersTableRepository;
        private readonly IGoogleFitClient _googleFitClient;
        private readonly ILogger<UsersService> _logger;
        private readonly IUsersKeyVaultRepository _usersKeyvaultRepository;
        private readonly IGoogleFitAuthService _authService;

        public UsersServiceTests()
        {
            _usersKeyvaultRepository = Substitute.For<IUsersKeyVaultRepository>();
            _googleFitClient = Substitute.For<IGoogleFitClient>();
            _authService = Substitute.For<IGoogleFitAuthService>();
            _usersTableRepository = Substitute.For<IUsersTableRepository>();
            _logger = NullLogger<UsersService>.Instance;
            _usersService = new UsersService(_usersTableRepository, _googleFitClient, _usersKeyvaultRepository, _authService, _logger);
        }

        [Fact]
        public async Task GivenTheAccessCodeIsNotValid_WhenInitiateIsCalled_ExceptionIsThrown()
        {
            await Task.Run(() => _authService.AuthTokensRequest(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((AuthTokensResponse)null));
            Task<Exception> exception = Assert.ThrowsAsync<Exception>(() => _usersService.Initiate("test", CancellationToken.None));
            Assert.Equal(exception.Result.Message, new Exception("Token response empty").Message);
        }

        [Fact]
        public async Task GivenTheEmailIsNotValid_WhenInitiateIsCalled_ExceptionIsThrown()
        {
            await Task.Run(() => _authService.AuthTokensRequest(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new AuthTokensResponse()));
            await Task.Run(() => _googleFitClient.MyEmailRequest(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new MyEmailResponse { EmailAddress = null }));
            Task<Exception> exception = Assert.ThrowsAsync<Exception>(() => _usersService.Initiate("test", CancellationToken.None));
            Assert.Equal(exception.Result.Message, new Exception("Email response empty").Message);
        }

        [Fact]
        public async Task GivenValidAccessTokenAndEmail_WhenInitiateIsCalled_ReturnValidUser()
        {
            await Task.Run(() => _authService.AuthTokensRequest(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new AuthTokensResponse { IdToken = GenerateToken("my-useraccount@site.com") }));
            await Task.Run(() => _googleFitClient.MyEmailRequest(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new MyEmailResponse { EmailAddress = "my-useraccount@site.com" }));
            await _usersTableRepository.Upsert(Arg.Any<User>(), Arg.Any<CancellationToken>());
            await _usersKeyvaultRepository.Upsert(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
            var actualResult = _usersService.Initiate("test", CancellationToken.None);

            var expectedResult = new User("my-useraccount@site.com");
            Assert.NotNull(actualResult.Result);
            Assert.Equal(actualResult.Result.Id, expectedResult.Id);
        }

        private System.IdentityModel.Tokens.Jwt.JwtSecurityToken GenerateToken(string userId)
        {
            var mySecret = "samplekey123456789";
            var mySecurityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.ASCII.GetBytes(mySecret));
            var myIssuer = "http://mysite.com";
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var claims = new[]
            {
                    new Claim(JwtRegisteredClaimNames.Sub, userId),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };
            var now = DateTime.UtcNow;
            var token = new JwtSecurityToken(
                  userId,
                  myIssuer,
                  claims,
                  now.AddMilliseconds(-30),
                  now.AddMinutes(60),
                  new SigningCredentials(mySecurityKey, SecurityAlgorithms.RsaSha256));

            return token;
        }
    }
}
