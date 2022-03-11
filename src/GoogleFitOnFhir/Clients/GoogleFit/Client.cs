// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using GoogleFitOnFhir.Clients.GoogleFit.Models;
using GoogleFitOnFhir.Clients.GoogleFit.Requests;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;

namespace GoogleFitOnFhir.Clients.GoogleFit
{
    public class Client
    {
        public Task<MyEmailResponse> MyEmailRequest(string accessToken)
        {
            return new MyEmailRequest(accessToken).ExecuteAsync();
        }

        public Task<DatasourcesListResponse> DatasourcesListRequest(string accessToken)
        {
            return new DatasourcesListRequest(accessToken)
                .ExecuteAsync();
        }

        public Task<IomtDataset> DatasetRequest(string accessToken, string dataStreamId, string dataSetId)
        {
            return new DatasetRequest(accessToken, dataStreamId, dataSetId)
                .ExecuteAsync();
        }
    }
}