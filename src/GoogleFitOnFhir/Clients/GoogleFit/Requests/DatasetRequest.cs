// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Google.Apis.Fitness.v1;
using GoogleFitOnFhir.Clients.GoogleFit.Models;

namespace GoogleFitOnFhir.Clients.GoogleFit.Requests
{
    public class DatasetRequest : BaseFitnessRequest
    {
        private readonly DataSource _dataSource;
        private readonly string _datasetId;

        public DatasetRequest(string accessToken, DataSource dataSource, string datasetId)
        : base(accessToken)
        {
            _dataSource = EnsureArg.IsNotNull(dataSource, nameof(dataSource));
            _datasetId = EnsureArg.IsNotNullOrWhiteSpace(datasetId, nameof(datasetId));
        }

        public async Task<MedTechDataset> ExecuteAsync(CancellationToken cancellationToken)
        {
            var datasourceRequest = new UsersResource.DataSourcesResource.DatasetsResource.GetRequest(
                FitnessService,
                "me",
                _dataSource.DataStreamId,
                _datasetId);

            var dataset = await datasourceRequest.ExecuteAsync(cancellationToken);

            return new MedTechDataset(dataset, _dataSource);
        }
    }
}