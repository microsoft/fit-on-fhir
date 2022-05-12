// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using EnsureThat;
using GoogleFitOnFhir.Clients.GoogleFit;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;
using GoogleFitOnFhir.Repositories;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GoogleFitOnFhir.Services
{
    public class GoogleFitDataImporter : IGoogleFitDataImporter
    {
        private readonly IUsersTableRepository _usersTableRepository;
        private readonly IGoogleFitClient _googleFitClient;
        private readonly GoogleFitImportService _googleFitImportService;
        private readonly ILogger<GoogleFitDataImporter> _logger;
        private readonly IUsersKeyVaultRepository _usersKeyvaultRepository;
        private readonly IGoogleFitAuthService _googleFitAuthService;

        public GoogleFitDataImporter(
            IUsersTableRepository usersTableRepository,
            IGoogleFitClient googleFitClient,
            GoogleFitImportService googleFitImportService,
            IUsersKeyVaultRepository usersKeyvaultRepository,
            IGoogleFitAuthService googleFitAuthService,
            ILogger<GoogleFitDataImporter> logger)
        {
            _usersTableRepository = EnsureArg.IsNotNull(usersTableRepository, nameof(usersTableRepository));
            _googleFitClient = EnsureArg.IsNotNull(googleFitClient, nameof(googleFitClient));
            _googleFitImportService = EnsureArg.IsNotNull(googleFitImportService, nameof(googleFitImportService));
            _usersKeyvaultRepository = EnsureArg.IsNotNull(usersKeyvaultRepository, nameof(usersKeyvaultRepository));
            _googleFitAuthService = EnsureArg.IsNotNull(googleFitAuthService, nameof(googleFitAuthService));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        public async Task Import(string userId, CancellationToken cancellationToken)
        {
            string refreshToken;

            _logger.LogInformation("Get RefreshToken from KV for {0}", userId);

            try
            {
                refreshToken = await _usersKeyvaultRepository.GetByName(userId, cancellationToken);
            }
            catch (AggregateException ex)
            {
                _logger.LogError(ex.Message);
                return;
            }

            _logger.LogInformation("Refreshing the RefreshToken");
            AuthTokensResponse tokensResponse = await _googleFitAuthService.RefreshTokensRequest(refreshToken, cancellationToken);

            if (!string.IsNullOrEmpty(tokensResponse.RefreshToken))
            {
                _logger.LogInformation("Updating refreshToken in KV for {0}", userId);
                await _usersKeyvaultRepository.Upsert(userId, tokensResponse.RefreshToken, cancellationToken);
            }
            else
            {
                _logger.LogInformation("RefreshToken is empty for {0}", userId);
            }

            _logger.LogInformation("Execute GoogleFitClient.DataSourcesListRequest");
            var dataSourcesList = await _googleFitClient.DatasourcesListRequest(tokensResponse.AccessToken, cancellationToken);

            // Get user's info for LastSync date
            _logger.LogInformation("Query userInfo");
            var user = await _usersTableRepository.GetById(userId, cancellationToken);

            // Generating datasetId based on event type
            DateTime startDateDt = DateTime.Now.AddDays(-30);
            DateTimeOffset startDateDto = new DateTimeOffset(startDateDt);
            if (user.LastSync != null)
            {
                startDateDto = user.LastSync.Value;
            }

            // Convert to DateTimeOffset to so .NET unix conversion is usable
            DateTimeOffset endDateDto = new DateTimeOffset(DateTime.Now);

            // .NET unix conversion only goes as small as milliseconds, multiplying to get nanoseconds
            var startDate = startDateDto.ToUnixTimeMilliseconds() * 1000000;
            var endDate = endDateDto.ToUnixTimeMilliseconds() * 1000000;
            var datasetId = startDate + "-" + endDate;

            // Request the datasets from each datasource, based on the datasetId
            await _googleFitImportService.ProcessDatasetRequests(user, dataSourcesList.DatasourceIds, datasetId, tokensResponse, cancellationToken);

            // Update LastSync column
            user.LastSync = endDateDto;
            await _usersTableRepository.Update(user, cancellationToken);
        }
    }
}
