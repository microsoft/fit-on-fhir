// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using FitOnFhir.Common.Exceptions;
using FitOnFhir.Common.Interfaces;
using FitOnFhir.Common.Models;
using FitOnFhir.Common.Repositories;
using FitOnFhir.Common.Services;
using FitOnFhir.GoogleFit.Client.Models;
using FitOnFhir.GoogleFit.Client.Responses;
using FitOnFhir.GoogleFit.Common;
using FitOnFhir.GoogleFit.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Extensions.Fhir.Service;
using Newtonsoft.Json;

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
            _utcNowFunc = utcNowFunc;
            _logger = logger;
        }

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

        public override async Task QueueFitnessImport(User user, CancellationToken cancellationToken)
        {
            var googleUserInfo = user.GetPlatformUserInfo().FirstOrDefault(usr => usr.PlatformName == GoogleFitConstants.GoogleFitPlatformName);
            if (googleUserInfo != null)
            {
                await _queueService.SendQueueMessage(user.Id, googleUserInfo.UserId, googleUserInfo.PlatformName, cancellationToken);
            }
        }

        public override async Task RevokeAccess(string patientId, CancellationToken cancellationToken)
        {
            var user = await RetrieveUserForPatient(patientId, GoogleFitConstants.GoogleFitPlatformName, cancellationToken);

            if (user != null)
            {
                var platformInfo = user.GetPlatformUserInfo()
                    .FirstOrDefault(pui => pui.PlatformName == GoogleFitConstants.GoogleFitPlatformName);

                if (platformInfo != default)
                {
                    AuthTokensResponse tokensResponse;
                    try
                    {
                        tokensResponse = await _googleFitTokensService.RefreshToken(platformInfo.UserId, cancellationToken);
                    }
                    catch (TokenRefreshException ex)
                    {
                        _logger.LogError(ex, ex.Message);
                        return;
                    }

                    await _authService.RevokeTokenRequest(tokensResponse.AccessToken, cancellationToken);

                    // update the revoke reason and timestamp for this user
                    platformInfo.RevokedAccessReason = RevokeReason.UserInitiated;
                    platformInfo.RevokedTimeStamp = _utcNowFunc();
                    await UpdateUser(user, cancellationToken);
                }
            }
        }
    }
}
