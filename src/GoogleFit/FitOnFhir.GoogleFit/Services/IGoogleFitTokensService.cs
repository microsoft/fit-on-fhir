// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.Common.Interfaces;
using FitOnFhir.GoogleFit.Client.Responses;

namespace FitOnFhir.GoogleFit.Services
{
    public interface IGoogleFitTokensService : ITokensService<AuthTokensResponse>
    {
    }
}
