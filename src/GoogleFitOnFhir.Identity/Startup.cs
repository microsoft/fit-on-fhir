// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using GoogleFitOnFhir.Clients.GoogleFit;
using GoogleFitOnFhir.Clients.GoogleFit.Handlers;
using GoogleFitOnFhir.Common;
using GoogleFitOnFhir.Persistence;
using GoogleFitOnFhir.Repositories;
using GoogleFitOnFhir.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Common.Handler;
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

            builder.Services.AddLogging();

            builder.Services.AddSingleton(sp => new GoogleFitClientContext(googleFitClientId, googleFitClientSecret, Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME")));
            builder.Services.AddSingleton(sp => new StorageAccountContext(storageAccountConnectionString));
            builder.Services.AddSingleton(sp => new SecretClient(new Uri(usersKeyVaultUri), new DefaultAzureCredential()));

            builder.Services.AddSingleton<IGoogleFitClient, GoogleFitClient>();
            builder.Services.AddSingleton<IUsersKeyVaultRepository, UsersKeyVaultRepository>();
            builder.Services.AddSingleton<IUsersTableRepository, UsersTableRepository>();
            builder.Services.AddSingleton<IUsersService, UsersService>();
            builder.Services.AddSingleton<IAuthService, AuthService>();
            builder.Services.AddSingleton<IRoutingService, RoutingService>();
            builder.Services.AddSingleton(sp => CreateOrderedHandlerChain(sp, typeof(GoogleFitHandler), typeof(UnknownOperationHandler)));
        }

        internal static IResponsibilityHandler<RoutingRequest, Task<IActionResult>> CreateOrderedHandlerChain(IServiceProvider serviceProvider, params Type[] handlerTypes)
        {
            IResponsibilityHandler<RoutingRequest, Task<IActionResult>> previousHandler = null;

            // Loop through the handlerTypes retrieve the instance and chain then together.
            foreach (Type handlerType in handlerTypes)
            {
                IResponsibilityHandler<RoutingRequest, Task<IActionResult>> handler = serviceProvider.GetRequiredService(handlerType) as IResponsibilityHandler<RoutingRequest, Task<IActionResult>>;
                if (previousHandler != null)
                {
                    previousHandler.Chain(handler);
                }

                previousHandler = handler;
            }

            // Return the first handler. This will register the first handler in the chain with the Service Provider
            return serviceProvider.GetRequiredService(handlerTypes[0]) as IResponsibilityHandler<RoutingRequest, Task<IActionResult>>;
        }
    }
}
