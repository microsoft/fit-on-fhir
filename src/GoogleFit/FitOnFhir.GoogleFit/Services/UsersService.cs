// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Extensions.Fhir.Service;
using Microsoft.Health.FitOnFhir.Common.Interfaces;
using Microsoft.Health.FitOnFhir.Common.Models;
using Microsoft.Health.FitOnFhir.Common.Repositories;
using Microsoft.Health.FitOnFhir.Common.Services;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Models;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Responses;
using Microsoft.Health.FitOnFhir.GoogleFit.Common;
using Microsoft.Health.FitOnFhir.GoogleFit.Repositories;
using Newtonsoft.Json;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Services
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
        private readonly IGoogleFitTokensService _googleFitTokensService;
        private readonly Func<DateTimeOffset> _utcNowFunc;
        private readonly ILogger<UsersService> _logger;

        public UsersService(
            ResourceManagementService resourceManagementService,
            IUsersTableRepository usersTableRepository,
            IGoogleFitUserTableRepository googleFitUserRepository,
            IUsersKeyVaultRepository usersKeyVaultRepository,
            IGoogleFitAuthService authService,
            IQueueService queueService,
            IGoogleFitTokensService googleFitTokensService,
            Func<DateTimeOffset> utcNowFunc,
            ILogger<UsersService> logger)
            : base(resourceManagementService, usersTableRepository)
        {
            _googleFitUserRepository = EnsureArg.IsNotNull(googleFitUserRepository, nameof(googleFitUserRepository));
            _usersKeyVaultRepository = EnsureArg.IsNotNull(usersKeyVaultRepository, nameof(usersKeyVaultRepository));
            _authService = EnsureArg.IsNotNull(authService, nameof(authService));
            _queueService = EnsureArg.IsNotNull(queueService, nameof(queueService));
            _googleFitTokensService = EnsureArg.IsNotNull(googleFitTokensService, nameof(googleFitTokensService));
            _utcNowFunc = EnsureArg.IsNotNull(utcNowFunc);
            _logger = logger;
        }

        public override string PlatformName => GoogleFitConstants.GoogleFitPlatformName;

        /// <inheritdoc/>
        public async Task ProcessAuthorizationCallback(string authCode, string state, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(authCode))
            {
                // This exception message will be visible to the caller.
                throw new Exception("The Google authorization service failed to return an access code.");
            }

            AuthState authState;

            try
            {
                authState = AuthState.Parse(state);
            }
            catch (Exception e) when (e is ArgumentException || e is JsonSerializationException)
            {
                // This exception message will be visible to the caller.
                throw new Exception("The Google authorization service failed to return the expected authorization state.");
            }

            // Exchange the code for Auth, Refresh and Id tokens.
            var tokenResponse = await _authService.AuthTokensRequest(authCode, cancellationToken);

            if (tokenResponse == null)
            {
                // This exception message will be visible to the caller.
                throw new Exception("This Google user has already authorized data sharing.");
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
            await EnsurePatientAndUser(
                GoogleFitConstants.GoogleFitPlatformName,
                googleUserId,
                tokenIssuer,
                authState,
                cancellationToken);

            GoogleFitUser googleFitUser = await _googleFitUserRepository.GetById(googleUserId, cancellationToken);

            if (googleFitUser == null)
            {
                // Insert GoogleFitUser into Users Table
                await _googleFitUserRepository.Insert(new GoogleFitUser(googleUserId), cancellationToken);
            }

            // Insert refresh token into users KV by userId
            await _usersKeyVaultRepository.Upsert(googleUserId, tokenResponse.RefreshToken, cancellationToken);
        }

        /// <inheritdoc/>
        public override async Task QueueFitnessImport(User user, CancellationToken cancellationToken)
        {
            var googleUserInfo = user.GetPlatformUserInfo().FirstOrDefault(usr => usr.PlatformName == GoogleFitConstants.GoogleFitPlatformName);
            if (googleUserInfo != null)
            {
                await _queueService.SendQueueMessage(user.Id, googleUserInfo.UserId, googleUserInfo.PlatformName, cancellationToken);
            }
        }

        /// <inheritdoc/>
        public override async Task RevokeAccessRequest(PlatformUserInfo platformUserInfo, CancellationToken cancellationToken)
        {
            if (platformUserInfo != default)
            {
                AuthTokensResponse tokensResponse = await _googleFitTokensService.RefreshToken(platformUserInfo.UserId, cancellationToken);

                await _authService.RevokeTokenRequest(tokensResponse.AccessToken, cancellationToken);

                // update the revoke reason and timestamp for this user
                platformUserInfo.RevokedAccessReason = RevokeReason.UserInitiated;
                platformUserInfo.RevokedTimeStamp = _utcNowFunc();
                platformUserInfo.ImportState = DataImportState.Unauthorized;
            }
        }
    }
}
