// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Fitness.v1;
using GoogleFitOnFhir.Clients.GoogleFit.Models;

namespace GoogleFitOnFhir.Clients.GoogleFit.Requests
{
    public class DatasetRequest : BaseFitnessRequest
    {
        private readonly string _dataStreamId;
        private readonly string _datasetId;
        private readonly string _pageToken;
        private readonly int _limit;

        public DatasetRequest(string accessToken, string dataStreamId, string datasetId, int limit, string pageToken = null)
        : base(accessToken)
        {
            _dataStreamId = dataStreamId;
            _datasetId = datasetId;
            _limit = limit;
            _pageToken = pageToken;
        }

        public async Task<IomtDataset> ExecuteAsync(CancellationToken cancellationToken)
        {
            IList<IomtDataset> iomtDatasets = new List<IomtDataset>();

            var datasourceRequest = new UsersResource.DataSourcesResource.DatasetsResource.GetRequest(
                FitnessService,
                "me",
                _dataStreamId,
                _datasetId) { Limit = _limit, PageToken = _pageToken };

            var result = await datasourceRequest.ExecuteAsync(cancellationToken);
            return new IomtDataset(result);
        }
    }
}