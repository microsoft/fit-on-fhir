// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Google.Apis.Fitness.v1;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Models;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Client.Requests
{
    public class DatasetRequest : BaseFitnessRequest
    {
        private readonly DataSource _dataSource;
        private readonly string _datasetId;
        private readonly string _pageToken;
        private readonly int _limit;

        public DatasetRequest(string accessToken, DataSource dataSource, string datasetId, int limit, string pageToken = null)
        : base(accessToken)
        {
            _dataSource = EnsureArg.IsNotNull(dataSource, nameof(dataSource));
            _datasetId = EnsureArg.IsNotNullOrWhiteSpace(datasetId, nameof(datasetId));
            _limit = limit;
            _pageToken = pageToken;
        }

        public async Task<MedTechDataset> ExecuteAsync(CancellationToken cancellationToken)
        {
            var datasourceRequest = new UsersResource.DataSourcesResource.DatasetsResource.GetRequest(
                FitnessService,
                "me",
                _dataSource.DataStreamId,
                _datasetId) { Limit = _limit, PageToken = _pageToken };

            var dataset = await datasourceRequest.ExecuteAsync(cancellationToken);

            if (dataset == null || !dataset.Point.Any())
            {
                return null;
            }

            return new MedTechDataset(dataset, _dataSource);
        }
    }
}