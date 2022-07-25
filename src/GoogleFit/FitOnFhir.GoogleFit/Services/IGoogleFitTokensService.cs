// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.FitOnFhir.Common.Interfaces;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Responses;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Services
{
    public interface IGoogleFitTokensService : ITokensService<AuthTokensResponse>
    {
    }
}
