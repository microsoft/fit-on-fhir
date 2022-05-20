// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using FitOnFhir.GoogleFit.Persistence;
using FitOnFhir.GoogleFit.Repositories;
using FitOnFhir.ImportTimerTrigger;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace FitOnFhir.ImportTimerTrigger
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            string storageAccountConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            builder.Services.AddSingleton(sp => new StorageAccountContext(storageAccountConnectionString));

            builder.Services.AddSingleton<IUsersTableRepository, UsersTableRepository>();
        }
    }
}