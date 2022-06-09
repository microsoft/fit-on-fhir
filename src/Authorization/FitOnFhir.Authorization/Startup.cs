// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using FitOnFhir.Authorization;
using FitOnFhir.Authorization.Services;
using FitOnFhir.Common;
using FitOnFhir.Common.Config;
using FitOnFhir.Common.ExtensionMethods;
using FitOnFhir.Common.Handlers;
using FitOnFhir.Common.Persistence;
using FitOnFhir.Common.Repositories;
using FitOnFhir.Common.Requests;
using FitOnFhir.GoogleFit.Client;
using FitOnFhir.GoogleFit.Client.Config;
using FitOnFhir.GoogleFit.Client.Handlers;
using FitOnFhir.GoogleFit.Repositories;
using FitOnFhir.GoogleFit.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Common.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace FitOnFhir.Authorization
{
    public class Startup : StartupBase
    {
        public override void Configure(IFunctionsHostBuilder builder, IConfiguration configuration)
        {
            builder.Services.AddLogging();
            builder.Services.AddConfiguration<GoogleFitAuthorizationConfiguration>(configuration);
            builder.Services.AddConfiguration<AzureConfiguration>(configuration);

            builder.Services.AddSingleton(sp =>
            {
                var azureConfiguration = sp.GetService<AzureConfiguration>();
                return new StorageAccountContext(azureConfiguration.AzureWebJobsStorage);
            });
            builder.Services.AddSingleton(sp =>
            {
                var azureConfiguration = sp.GetService<AzureConfiguration>();
                return new SecretClient(new Uri(azureConfiguration.UsersKeyVaultUri), new DefaultAzureCredential());
            });
            builder.Services.AddSingleton<IGoogleFitClient, GoogleFitClient>();
            builder.Services.AddSingleton<IGoogleFitDataImporter, GoogleFitDataImporter>();
            builder.Services.AddSingleton<IUsersKeyVaultRepository, UsersKeyVaultRepository>();
            builder.Services.AddSingleton<IUsersTableRepository, UsersTableRepository>();
            builder.Services.AddSingleton<IGoogleFitUserTableRepository, GoogleFitUserTableRepository>();
            builder.Services.AddSingleton<IUsersService, UsersService>();
            builder.Services.AddSingleton<IGoogleFitAuthService, GoogleFitAuthService>();
            builder.Services.AddSingleton<IRoutingService, RoutingService>();
            builder.Services.AddSingleton<GoogleFitAuthorizationHandler>();
            builder.Services.AddSingleton<UnknownAuthorizationHandler>();
            builder.Services.AddSingleton(sp => sp.CreateOrderedHandlerChain<RoutingRequest, Task<IActionResult>>(typeof(GoogleFitAuthorizationHandler), typeof(UnknownAuthorizationHandler)));
        }
    }
}
