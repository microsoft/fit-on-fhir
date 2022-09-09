// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.FitOnFhir.Common.Exceptions;
using Microsoft.Health.FitOnFhir.Common.Models;
using Microsoft.Health.FitOnFhir.Common.Repositories;
using Microsoft.Health.FitOnFhir.Common.Resolvers;
using Microsoft.Health.FitOnFhir.GoogleFit.Client;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Responses;
using Microsoft.Health.FitOnFhir.GoogleFit.Common;
using Microsoft.Health.FitOnFhir.GoogleFit.Repositories;
using Microsoft.Health.FitOnFhir.GoogleFit.Resolvers;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Services
{
    public class GoogleFitDataImporter : IGoogleFitDataImporter
    {
        private readonly IUsersTableRepository _usersTableRepository;
        private readonly IGoogleFitUserTableRepository _googleFitUserTableRepository;
        private readonly IGoogleFitClient _googleFitClient;
        private readonly IGoogleFitImportService _googleFitImportService;
        private readonly ILogger<GoogleFitDataImporter> _logger;
        private readonly IGoogleFitTokensService _googleFitTokensService;
        private readonly Func<DateTimeOffset> _utcNowFunc;

        public GoogleFitDataImporter(
            IUsersTableRepository usersTableRepository,
            IGoogleFitUserTableRepository googleFitUserTableRepository,
            IGoogleFitClient googleFitClient,
            IGoogleFitImportService googleFitImportService,
            IGoogleFitTokensService googleFitTokensService,
            Func<DateTimeOffset> utcNowFunc,
            ILogger<GoogleFitDataImporter> logger)
        {
            _usersTableRepository = EnsureArg.IsNotNull(usersTableRepository, nameof(usersTableRepository));
            _googleFitUserTableRepository = EnsureArg.IsNotNull(googleFitUserTableRepository, nameof(googleFitUserTableRepository));
            _googleFitClient = EnsureArg.IsNotNull(googleFitClient, nameof(googleFitClient));
            _googleFitImportService = EnsureArg.IsNotNull(googleFitImportService, nameof(googleFitImportService));
            _googleFitTokensService = EnsureArg.IsNotNull(googleFitTokensService, nameof(googleFitTokensService));
            _utcNowFunc = EnsureArg.IsNotNull(utcNowFunc);
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        /// <inheritdoc/>
        public async Task Import(string userId, string googleFitId, CancellationToken cancellationToken)
        {
            AuthTokensResponse tokensResponse;

            // Get user's info for LastSync date
            _logger.LogInformation("Query userInfo for user: {0}, platformId: {1}", userId, googleFitId);
            var user = await _usersTableRepository.GetById(userId, cancellationToken);
            user = await UpdateUserAndImportState(user, DataImportState.Importing, cancellationToken);

            try
            {
                tokensResponse = await _googleFitTokensService.RefreshToken(googleFitId, cancellationToken);
            }
            catch (TokenRefreshException ex)
            {
                _logger.LogError(ex, ex.Message);
                await UpdateUserAndImportState(user, DataImportState.Unauthorized, cancellationToken);
                return;
            }

            if (tokensResponse?.AccessToken != default)
            {
                // Get DataSources list for this user
                _logger.LogInformation("Execute GoogleFitClient.DataSourcesListRequest for user: {0}, platformId: {1}", userId, googleFitId);
                var dataSourcesList = await _googleFitClient.DataSourcesListRequest(tokensResponse.AccessToken, cancellationToken);

                // Get user sync times
                _logger.LogInformation("Query userInfo for user: {0}, platformId: {1}", userId, googleFitId);
                var googleUser = await _googleFitUserTableRepository.GetById(googleFitId, cancellationToken);

#pragma warning disable CA1031 // Do not catch general exception types
                try
                {
                    // Request the datasets from each datasource, based on the datasetId
                    await _googleFitImportService.ProcessDatasetRequests(googleUser, dataSourcesList.DataSources, tokensResponse, cancellationToken);

                    // Persist the last sync times if no exceptions occur in the import service.
                    // This ensures if an error happens during processing, the dataset will be tried again the next import.
                    await _googleFitUserTableRepository.Update(
                        googleUser,
                        GoogleFitUserConflictResolvers.ResolveConflictLastSyncTimes,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }

            // Update the user as ready to import for GoogleFit
            await UpdateUserAndImportState(user, DataImportState.ReadyToImport, cancellationToken);

            _logger.LogInformation("Import finalized: {0}, platformId", userId, googleFitId);
        }

        private async Task<User> UpdateUserAndImportState(User user, DataImportState dataImportState, CancellationToken cancellationToken)
        {
            user.LastTouched = _utcNowFunc();
            user.UpdateImportState(GoogleFitConstants.GoogleFitPlatformName, dataImportState);
            return await _usersTableRepository.Update(
                user,
                UserConflictResolvers.ResolveConflictDefault,
                cancellationToken);
        }
    }
}
