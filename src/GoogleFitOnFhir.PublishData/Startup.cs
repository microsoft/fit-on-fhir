// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.EventHubs.Producer;
using Azure.Security.KeyVault.Secrets;
using GoogleFitOnFhir.Clients.GoogleFit;
using GoogleFitOnFhir.Clients.GoogleFit.Handlers;
using GoogleFitOnFhir.Common;
using GoogleFitOnFhir.Identity;
using GoogleFitOnFhir.Persistence;
using GoogleFitOnFhir.Repositories;
using GoogleFitOnFhir.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

using GoogleFitClientContext = GoogleFitOnFhir.Clients.GoogleFit.GoogleFitClientContext;

[assembly: FunctionsStartup(typeof(GoogleFitOnFhir.PublishData.Startup))]

namespace GoogleFitOnFhir.PublishData
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            string iomtConnectionString = Environment.GetEnvironmentVariable("EventHubConnectionString");
            string storageAccountConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            string usersKeyVaultUri = Environment.GetEnvironmentVariable("USERS_KEY_VAULT_URI");
            string googleFitClientId = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_ID");
            string googleFitClientSecret = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_SECRET");
            string hostName = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");

            builder.Services.AddLogging();
            builder.Services.AddSingleton(sp => new GoogleFitClientContext(googleFitClientId, googleFitClientSecret, hostName));
            builder.Services.AddSingleton(sp => new StorageAccountContext(storageAccountConnectionString));
            builder.Services.AddScoped(sp => new EventHubProducerClient(iomtConnectionString));
            builder.Services.AddSingleton(sp => new SecretClient(new Uri(usersKeyVaultUri), new DefaultAzureCredential()));

            builder.Services.AddSingleton<IGoogleFitClient, GoogleFitClient>();
            builder.Services.AddSingleton<IUsersKeyVaultRepository, UsersKeyVaultRepository>();
            builder.Services.AddSingleton<IGoogleFitAuthService, GoogleFitAuthService>();
            builder.Services.AddSingleton<IUsersTableRepository, UsersTableRepository>();
            builder.Services.AddScoped<IUsersService, UsersService>();
            builder.Services.AddSingleton<IPublisherService, PublisherService>();
            builder.Services.AddSingleton<GoogleFitPublishingHandler>();
            builder.Services.AddSingleton<UnknownGoogleFitPublisherHandler>();
            builder.Services.AddScoped<IGoogleFitDataImporter, GoogleFitDataImporter>();
            builder.Services.AddSingleton(sp => sp.CreateOrderedHandlerChain<PublishRequest, Task>(typeof(GoogleFitPublishingHandler), typeof(UnknownGoogleFitPublisherHandler)));
        }
    }
}
