// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Google.Apis.Fitness.v1;
using GoogleFitOnFhir.Clients.GoogleFit.Models;

namespace GoogleFitOnFhir.Clients.GoogleFit.Requests
{
    public class DatasetRequest : BaseFitnessRequest
    {
        private readonly string _dataStreamId;
        private readonly string _datasetId;

        public DatasetRequest(string accessToken, string dataStreamId, string datasetId)
        : base(accessToken)
        {
            _dataStreamId = dataStreamId;
            _datasetId = datasetId;
        }

        public async Task<IomtDataset> ExecuteAsync()
        {
            var datasourceRequest = new UsersResource.DataSourcesResource.DatasetsResource.GetRequest(
                FitnessService,
                "me",
                _dataStreamId,
                _datasetId);
            var result = await datasourceRequest.ExecuteAsync();
            return new IomtDataset(result);
        }
    }
}