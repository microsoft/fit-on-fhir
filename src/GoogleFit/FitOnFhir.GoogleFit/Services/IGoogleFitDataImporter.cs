// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace FitOnFhir.GoogleFit.Services
{
    public interface IGoogleFitDataImporter
    {
        Task Import(string userId, CancellationToken cancellationToken);
    }
}
