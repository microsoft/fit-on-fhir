// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.FitOnFhir.Common.Exceptions;
using Microsoft.Health.FitOnFhir.Common.Repositories;
using Microsoft.Health.FitOnFhir.Common.Services;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Responses;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Services
{
    public class GoogleFitTokensService : TokensServiceBase<AuthTokensResponse>, IGoogleFitTokensService
    {
        private readonly IGoogleFitAuthService _googleFitAuthService;

        public GoogleFitTokensService(
            IGoogleFitAuthService googleFitAuthService,
            IUsersKeyVaultRepository usersKeyVaultRepository,
            ILogger<GoogleFitTokensService> logger)
        : base(usersKeyVaultRepository, logger)
        {
            _googleFitAuthService = EnsureArg.IsNotNull(googleFitAuthService, nameof(googleFitAuthService));
        }

        protected override async Task<AuthTokensResponse> UpdateRefreshToken(string refreshToken, CancellationToken cancellationToken)
        {
            try
            {
                return await _googleFitAuthService.RefreshTokensRequest(refreshToken, cancellationToken);
            }
            catch (Google.Apis.Auth.OAuth2.Responses.TokenResponseException ex)
            {
                throw new TokenRefreshException(ex.Message);
            }
        }
    }
}
