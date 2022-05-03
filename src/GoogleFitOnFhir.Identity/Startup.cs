// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using GoogleFitOnFhir.Clients.GoogleFit;
using GoogleFitOnFhir.Clients.GoogleFit.Handlers;
using GoogleFitOnFhir.Common;
using GoogleFitOnFhir.Persistence;
using GoogleFitOnFhir.Repositories;
using GoogleFitOnFhir.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using GoogleFitClientContext = GoogleFitOnFhir.Clients.GoogleFit.GoogleFitClientContext;

[assembly: FunctionsStartup(typeof(GoogleFitOnFhir.Identity.Startup))]

namespace GoogleFitOnFhir.Identity
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            string storageAccountConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            string usersKeyVaultUri = Environment.GetEnvironmentVariable("USERS_KEY_VAULT_URI");
            string googleFitClientId = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_ID");
            string googleFitClientSecret = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_SECRET");
            string hostName = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");

            builder.Services.AddLogging();

            builder.Services.AddSingleton(sp => new GoogleFitClientContext(googleFitClientId, googleFitClientSecret, hostName));
            builder.Services.AddSingleton(sp => new StorageAccountContext(storageAccountConnectionString));
            builder.Services.AddSingleton(sp => new SecretClient(new Uri(usersKeyVaultUri), new DefaultAzureCredential()));

            builder.Services.AddSingleton<IGoogleFitClient, GoogleFitClient>();
            builder.Services.AddSingleton<IUsersKeyVaultRepository, UsersKeyVaultRepository>();
            builder.Services.AddSingleton<IUsersTableRepository, UsersTableRepository>();
            builder.Services.AddSingleton<IUsersService, UsersService>();
            builder.Services.AddSingleton<IGoogleFitAuthService, GoogleFitAuthService>();
            builder.Services.AddSingleton<IRoutingService, RoutingService>();
            builder.Services.AddSingleton<GoogleFitHandler>();
            builder.Services.AddSingleton<UnknownOperationHandler>();
            builder.Services.AddSingleton(sp => sp.CreateOrderedHandlerChain(typeof(GoogleFitHandler), typeof(UnknownOperationHandler)));
        }
    }
}
