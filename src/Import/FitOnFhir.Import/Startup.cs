// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Common.DependencyInjection;
using Microsoft.Health.FitOnFhir.Common;
using Microsoft.Health.FitOnFhir.Common.ExtensionMethods;
using Microsoft.Health.FitOnFhir.Common.Handlers;
using Microsoft.Health.FitOnFhir.Common.Interfaces;
using Microsoft.Health.FitOnFhir.Common.Providers;
using Microsoft.Health.FitOnFhir.Common.Repositories;
using Microsoft.Health.FitOnFhir.Common.Requests;
using Microsoft.Health.FitOnFhir.Common.Services;
using Microsoft.Health.FitOnFhir.GoogleFit.Client;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Config;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Handlers;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Telemetry;
using Microsoft.Health.FitOnFhir.GoogleFit.Repositories;
using Microsoft.Health.FitOnFhir.GoogleFit.Services;
using Microsoft.Health.FitOnFhir.Import;
using Microsoft.Health.Logging.Telemetry;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Microsoft.Health.FitOnFhir.Import
{
    public class Startup : StartupBase
    {
        public override void Configure(IFunctionsHostBuilder builder, IConfiguration configuration)
        {
            EnsureArg.IsNotNull(builder, nameof(builder));

            builder.Services.AddConfiguration<GoogleFitAuthorizationConfiguration>(configuration);
            builder.Services.AddConfiguration<GoogleFitDataImporterConfiguration>(configuration);
            builder.Services.AddSingleton<IGoogleFitClient, GoogleFitClient>();
            builder.Services.AddSingleton<ISecretClientProvider, SecretClientProvider>();
            builder.Services.AddSingleton<IUsersKeyVaultRepository, UsersKeyVaultRepository>();
            builder.Services.AddSingleton<IGoogleFitAuthService, GoogleFitAuthService>();
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
            builder.Services.AddSingleton<IGoogleFitTokensService, GoogleFitTokensService>();
            builder.Services.AddSingleton(typeof(Func<DateTimeOffset>), () => DateTimeOffset.UtcNow);
            builder.Services.AddSingleton(sp => sp.CreateOrderedHandlerChain<ImportRequest, Task<bool?>>(typeof(GoogleFitDataImportHandler), typeof(UnknownDataImportHandler)));
        }
    }
}
