// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using FitOnFhir.Common.Interfaces;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace FitOnFhir.Common
{
    public abstract class StartupBase : FunctionsStartup, IStartupBase
    {
        private IFunctionsHostBuilder _hostBuilder;

        public override void Configure(IFunctionsHostBuilder builder)
        {
            _hostBuilder = EnsureArg.IsNotNull(builder);

            IConfiguration config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            Configure(_hostBuilder, config);
        }

        public abstract void Configure(IFunctionsHostBuilder builder, IConfiguration configuration);
    }
}
