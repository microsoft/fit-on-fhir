// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.Common;
using FitOnFhir.GoogleFit.Repositories;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FitOnFhir.GoogleFit.Common
{
    public class GoogleFitStartupBase : StartupBase
    {
        public override void Configure(IFunctionsHostBuilder builder, IConfiguration configuration)
        {
            builder.Services.AddSingleton<IGoogleFitUserTableRepository, GoogleFitUserTableRepository>();
        }
    }
}
