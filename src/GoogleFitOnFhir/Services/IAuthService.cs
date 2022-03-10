// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;

namespace GoogleFitOnFhir.Services
{
    public interface IAuthService
    {
        Task<AuthUriResponse> AuthUriRequest();

        Task<AuthTokensResponse> AuthTokensRequest(string authCode);

        Task<AuthTokensResponse> RefreshTokensRequest(string refreshToken);
    }
}