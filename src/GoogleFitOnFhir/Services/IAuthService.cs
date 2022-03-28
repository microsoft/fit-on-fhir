// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;

namespace GoogleFitOnFhir.Services
{
    public interface IAuthService
    {
        Task<AuthUriResponse> AuthUriRequest(CancellationToken cancellationToken);

        Task<AuthTokensResponse> AuthTokensRequest(string authCode, CancellationToken cancellationToken);

        Task<AuthTokensResponse> RefreshTokensRequest(string refreshToken, CancellationToken cancellationToken);
    }
}