// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.EventHubs.Producer;
using Azure.Security.KeyVault.Secrets;
using FitOnFhir.Common;
using FitOnFhir.Common.ExtensionMethods;
using FitOnFhir.Common.Handlers;
using FitOnFhir.Common.Interfaces;
using FitOnFhir.Common.Persistence;
using FitOnFhir.Common.Repositories;
using FitOnFhir.Common.Requests;
using FitOnFhir.GoogleFit.Client;
using FitOnFhir.GoogleFit.Client.Config;
using FitOnFhir.GoogleFit.Client.Handlers;
using FitOnFhir.GoogleFit.Client.Telemetry;
using FitOnFhir.GoogleFit.Repositories;
using FitOnFhir.GoogleFit.Services;
using FitOnFhir.Import;
using FitOnFhir.Import.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Logging.Telemetry;
using GoogleFitClientContext = FitOnFhir.GoogleFit.Client.GoogleFitClientContext;

[assembly: FunctionsStartup(typeof(Startup))]

namespace FitOnFhir.Import
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
            builder.Services.AddSingleton(sp => new EventHubProducerClient(iomtConnectionString));
            builder.Services.AddSingleton(sp => new SecretClient(new Uri(usersKeyVaultUri), new DefaultAzureCredential()));

            builder.Services.AddSingleton<IGoogleFitClient, GoogleFitClient>();
            builder.Services.AddSingleton<IUsersKeyVaultRepository, UsersKeyVaultRepository>();
            builder.Services.AddSingleton<IGoogleFitAuthService, GoogleFitAuthService>();
            builder.Services.AddSingleton<IUsersTableRepository, UsersTableRepository>();
            builder.Services.AddSingleton<IGoogleFitUserTableRepository, GoogleFitUserTableRepository>();
            builder.Services.AddSingleton<IUsersService, UsersService>();
            builder.Services.AddSingleton<IErrorHandler, ErrorHandler>();
            builder.Services.AddSingleton<IImporterService, ImporterService>();
            builder.Services.AddSingleton<GoogleFitDataImportHandler>();
            builder.Services.AddSingleton<UnknownDataImportHandler>();
            builder.Services.AddSingleton<IGoogleFitImportService, GoogleFitImportService>();
            builder.Services.AddSingleton<GoogleFitImportOptions>();
            builder.Services.AddSingleton<GoogleFitExceptionTelemetryProcessor>();
            builder.Services.AddSingleton<ITelemetryLogger, TelemetryLogger>();
            builder.Services.AddSingleton<IGoogleFitDataImporter, GoogleFitDataImporter>();
            builder.Services.AddSingleton(typeof(Func<DateTimeOffset>), () => DateTimeOffset.UtcNow);
            builder.Services.AddSingleton(sp => sp.CreateOrderedHandlerChain<ImportRequest, Task<bool?>>(typeof(GoogleFitDataImportHandler), typeof(UnknownDataImportHandler)));
        }
    }
}
