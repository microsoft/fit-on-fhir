// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using AngleSharp.Common;
using EnsureThat;
using FitOnFhir.Common.Repositories;
using FitOnFhir.GoogleFit.Client;
using FitOnFhir.GoogleFit.Client.Responses;
using FitOnFhir.GoogleFit.Repositories;
using Microsoft.Extensions.Logging;

namespace FitOnFhir.GoogleFit.Services
{
    public class GoogleFitDataImporter : IGoogleFitDataImporter
    {
        private readonly IUsersTableRepository _usersTableRepository;
        private readonly IGoogleFitUserTableRepository _googleFitUserTableRepository;
        private readonly IGoogleFitClient _googleFitClient;
        private readonly IGoogleFitImportService _googleFitImportService;
        private readonly ILogger<GoogleFitDataImporter> _logger;
        private readonly IUsersKeyVaultRepository _usersKeyvaultRepository;
        private readonly IGoogleFitAuthService _googleFitAuthService;
        private readonly Func<DateTimeOffset> _utcNowFunc;

        public GoogleFitDataImporter(
            IUsersTableRepository usersTableRepository,
            IGoogleFitUserTableRepository googleFitUserTableRepository,
            IGoogleFitClient googleFitClient,
            IGoogleFitImportService googleFitImportService,
            IUsersKeyVaultRepository usersKeyvaultRepository,
            IGoogleFitAuthService googleFitAuthService,
            Func<DateTimeOffset> utcNowFunc,
            ILogger<GoogleFitDataImporter> logger)
        {
            _usersTableRepository = EnsureArg.IsNotNull(usersTableRepository, nameof(usersTableRepository));
            _googleFitUserTableRepository = EnsureArg.IsNotNull(googleFitUserTableRepository, nameof(googleFitUserTableRepository));
            _googleFitClient = EnsureArg.IsNotNull(googleFitClient, nameof(googleFitClient));
            _googleFitImportService = EnsureArg.IsNotNull(googleFitImportService, nameof(googleFitImportService));
            _usersKeyvaultRepository = EnsureArg.IsNotNull(usersKeyvaultRepository, nameof(usersKeyvaultRepository));
            _googleFitAuthService = EnsureArg.IsNotNull(googleFitAuthService, nameof(googleFitAuthService));
            _utcNowFunc = EnsureArg.IsNotNull(utcNowFunc);
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        /// <inheritdoc/>
        public async Task Import(string userId, string googleFitId, CancellationToken cancellationToken)
        {
            string refreshToken;

            _logger.LogInformation("Get RefreshToken from KV for {0}", googleFitId);

            try
            {
                refreshToken = await _usersKeyvaultRepository.GetByName(googleFitId, cancellationToken);
            }
            catch (AggregateException ex)
            {
                _logger.LogError(ex, ex.Message);
                return;
            }

            _logger.LogInformation("Refreshing the RefreshToken");
            AuthTokensResponse tokensResponse = await _googleFitAuthService.RefreshTokensRequest(refreshToken, cancellationToken);

            if (!string.IsNullOrEmpty(tokensResponse.RefreshToken))
            {
                _logger.LogInformation("Updating refreshToken in KV for {0}", googleFitId);
                await _usersKeyvaultRepository.Upsert(googleFitId, tokensResponse.RefreshToken, cancellationToken);
            }
            else
            {
                _logger.LogInformation("RefreshToken is empty for {0}", googleFitId);
            }

            _logger.LogInformation("Execute GoogleFitClient.DataSourcesListRequest");
            var dataSourcesList = await _googleFitClient.DataSourcesListRequest(tokensResponse.AccessToken, cancellationToken);

            // get user sync times
            _logger.LogInformation("Query userInfo");
            var googleUser = await _googleFitUserTableRepository.GetById(userId, cancellationToken);

            // Request the datasets from each datasource, based on the datasetId
            try
            {
                await _googleFitImportService.ProcessDatasetRequests(googleUser, dataSourcesList.DataSources, tokensResponse, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            await _googleFitUserTableRepository.Update(googleUser, cancellationToken);

            // Get user's info for LastSync date
            _logger.LogInformation("Query userInfo");
            var user = await _usersTableRepository.GetById(userId, cancellationToken);

            // Update LastSync column
            user.LastTouched = _utcNowFunc();
            await _usersTableRepository.Update(user, cancellationToken);
        }
    }
}
