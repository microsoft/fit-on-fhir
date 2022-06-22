// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using FitOnFhir.Common.Interfaces;
using FitOnFhir.Common.Models;
using FitOnFhir.Common.Repositories;
using FitOnFhir.Common.Services;
using FitOnFhir.GoogleFit.Client.Models;
using FitOnFhir.GoogleFit.Common;
using FitOnFhir.GoogleFit.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Extensions.Fhir.Service;

namespace FitOnFhir.GoogleFit.Services
{
    /// <summary>
    /// User Service.
    /// </summary>
    public class UsersService : UsersServiceBase, IUsersService
    {
        private readonly IGoogleFitUserTableRepository _googleFitUserRepository;
        private readonly IUsersKeyVaultRepository _usersKeyVaultRepository;
        private readonly IGoogleFitAuthService _authService;
        private readonly IQueueService _queueService;
        private readonly ILogger<UsersService> _logger;

        public UsersService(
            ResourceManagementService resourceManagementService,
            IUsersTableRepository usersTableRepository,
            IGoogleFitUserTableRepository googleFitUserRepository,
            IUsersKeyVaultRepository usersKeyVaultRepository,
            IGoogleFitAuthService authService,
            IQueueService queueService,
            ILogger<UsersService> logger)
            : base(resourceManagementService, usersTableRepository)
        {
            _googleFitUserRepository = EnsureArg.IsNotNull(googleFitUserRepository, nameof(googleFitUserRepository));
            _usersKeyVaultRepository = EnsureArg.IsNotNull(usersKeyVaultRepository, nameof(usersKeyVaultRepository));
            _authService = EnsureArg.IsNotNull(authService, nameof(authService));
            _queueService = EnsureArg.IsNotNull(queueService, nameof(queueService));
            _logger = logger;
        }

        public async Task ProcessAuthorizationCallback(string authCode, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(authCode))
            {
                _logger.LogInformation("ProcessAuthorizationCallback called with no auth code");
                return;
            }

            // Exchange the code for Auth, Refresh and Id tokens.
            var tokenResponse = await _authService.AuthTokensRequest(authCode, cancellationToken);

            if (tokenResponse == null)
            {
                throw new Exception("Token response empty");
            }

            // https://developers.google.com/identity/protocols/oauth2/openid-connect#an-id-tokens-payload
            // Use the IdToken sub (Subject) claim for the user id - From the Google docs:
            // "An identifier for the user, unique among all Google accounts and never reused.
            // A Google account can have multiple email addresses at different points in time, but the sub value is never changed.
            // Use sub within your application as the unique-identifier key for the user.
            // Maximum length of 255 case-sensitive ASCII characters."
            string googleUserId = tokenResponse.IdToken.Subject;
            string tokenIssuer = tokenResponse.IdToken.Issuer;

            // Create a Patient and User if this is the first time the user has authorized.
            await EnsurePatientAndUser(GoogleFitConstants.GoogleFitPlatformName, googleUserId, tokenIssuer, cancellationToken);

            // Insert GoogleFitUser into Users Table
            await _googleFitUserRepository.Upsert(new GoogleFitUser(googleUserId), cancellationToken);

            // Insert refresh token into users KV by userId
            await _usersKeyVaultRepository.Upsert(googleUserId, tokenResponse.RefreshToken, cancellationToken);
        }

        public override async Task QueueFitnessImport(User user, CancellationToken cancellationToken)
        {
            var googleUserInfo = user.GetPlatformUserInfo().FirstOrDefault(usr => usr.PlatformName == GoogleFitConstants.GoogleFitPlatformName);
            if (googleUserInfo != null)
            {
                await _queueService.SendQueueMessage(user.Id, googleUserInfo.UserId, googleUserInfo.PlatformName, cancellationToken);
            }
        }
    }
}
