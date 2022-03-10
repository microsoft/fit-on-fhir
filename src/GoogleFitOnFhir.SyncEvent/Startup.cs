// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using GoogleFitOnFhir.Persistence;
using GoogleFitOnFhir.Repositories;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(GoogleFitOnFhir.SyncEvent.Startup))]

namespace GoogleFitOnFhir.SyncEvent
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