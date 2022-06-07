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
    public class MockFaultyGoogleFitTokenService : GoogleFitTokensService
    {
        private readonly AuthTokensResponse _tokensResponse = default;

        private readonly IGoogleFitAuthService _googleFitAuthService;
        private readonly ILogger<GoogleFitTokensService> _logger;

        public MockFaultyGoogleFitTokenService()
            : base()
        {
        }

        public MockFaultyGoogleFitTokenService(
            IGoogleFitAuthService googleFitAuthService,
            IUsersKeyVaultRepository usersKeyVaultRepository,
            ILogger<GoogleFitTokensService> logger)
            : base(googleFitAuthService, usersKeyVaultRepository, logger)
        {
            _googleFitAuthService = EnsureArg.IsNotNull(googleFitAuthService, nameof(googleFitAuthService));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        protected override Task<AuthTokensResponse> UpdateRefreshToken(string refreshToken, CancellationToken cancellationToken)
        {
            return Task.FromResult(_tokensResponse);
        }
    }
}
