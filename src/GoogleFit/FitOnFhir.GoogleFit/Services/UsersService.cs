// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.Common.Models;
using FitOnFhir.Common.Repositories;
using FitOnFhir.GoogleFit.Client;
using FitOnFhir.GoogleFit.Client.Models;
using FitOnFhir.GoogleFit.Common;
using FitOnFhir.GoogleFit.Repositories;
using Microsoft.Extensions.Logging;

namespace FitOnFhir.GoogleFit.Services
{
    /// <summary>
    /// User Service.
    /// </summary>
    public class UsersService : IUsersService
    {
        private readonly IUsersTableRepository _usersTableRepository;
        private readonly IGoogleFitUserTableRepository _googleFitUserRepository;
        private readonly IGoogleFitClient _googleFitClient;
        private readonly ILogger<UsersService> _logger;
        private readonly IUsersKeyVaultRepository _usersKeyvaultRepository;
        private readonly IGoogleFitAuthService _authService;

        public UsersService(
            IUsersTableRepository usersTableRepository,
            IGoogleFitUserTableRepository googleFitUserRepository,
            IGoogleFitClient googleFitClient,
            IUsersKeyVaultRepository usersKeyvaultRepository,
            IGoogleFitAuthService authService,
            ILogger<UsersService> logger)
        {
            _usersTableRepository = usersTableRepository;
            _googleFitUserRepository = googleFitUserRepository;
            _googleFitClient = googleFitClient;
            _usersKeyvaultRepository = usersKeyvaultRepository;
            _authService = authService;
            _logger = logger;
        }

        public async Task<User> Initiate(string authCode, CancellationToken cancellationToken)
        {
            var tokenResponse = await _authService.AuthTokensRequest(authCode, cancellationToken);
            if (tokenResponse == null)
            {
                throw new Exception("Token response empty");
            }

            var emailResponse = await _googleFitClient.MyEmailRequest(tokenResponse.AccessToken, cancellationToken);
            if (emailResponse == null)
            {
                throw new Exception("Email response empty");
            }

            // https://developers.google.com/identity/protocols/oauth2/openid-connect#an-id-tokens-payload
            // Use the IdToken sub (Subject) claim for the user id - From the Google docs:
            // "An identifier for the user, unique among all Google accounts and never reused.
            // A Google account can have multiple email addresses at different points in time, but the sub value is never changed.
            // Use sub within your application as the unique-identifier key for the user.
            // Maximum length of 255 case-sensitive ASCII characters."
            string userId = tokenResponse.IdToken.Subject;

            // Create a new user and add GoogleFit info
            User user = new User(Guid.NewGuid());
            user.SavePlatformUserName(GoogleFitConstants.GoogleFitPlatformName, userId);

            // Insert user into Users Table
            await _usersTableRepository.Upsert(user, cancellationToken);

            GoogleFitUser googleFitUser = new GoogleFitUser(userId);

            // Insert GoogleFitUser into Users Table
            await _googleFitUserRepository.Upsert(googleFitUser, cancellationToken);

            // Insert refresh token into users KV by userId
            await _usersKeyvaultRepository.Upsert(userId, tokenResponse.RefreshToken, cancellationToken);

            return user;
        }

        public void QueueFitnessImport(User user)
        {
        }
    }
}
