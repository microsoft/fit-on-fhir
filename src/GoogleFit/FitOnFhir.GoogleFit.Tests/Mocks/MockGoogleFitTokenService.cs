// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using FitOnFhir.Common.Repositories;
using FitOnFhir.GoogleFit.Client.Responses;
using FitOnFhir.GoogleFit.Services;
using Microsoft.Extensions.Logging;

namespace FitOnFhir.GoogleFit.Tests.Mocks
{
    public class MockGoogleFitTokenService : GoogleFitTokensService
    {
        private const string _accessToken = "AccessToken";
        private const string _refreshToken = "RefreshToken";
        private readonly AuthTokensResponse _tokensResponse = new AuthTokensResponse() { AccessToken = _accessToken, RefreshToken = _refreshToken };

        public MockGoogleFitTokenService(
            IGoogleFitAuthService googleFitAuthService,
            IUsersKeyVaultRepository usersKeyVaultRepository,
            ILogger<GoogleFitTokensService> logger)
            : base(googleFitAuthService, usersKeyVaultRepository, logger)
        {
        }

        protected override Task<AuthTokensResponse> UpdateRefreshToken(string refreshToken, CancellationToken cancellationToken)
        {
            return Task.FromResult(_tokensResponse);
        }
    }
}
