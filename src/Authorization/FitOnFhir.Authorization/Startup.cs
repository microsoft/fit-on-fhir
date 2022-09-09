// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Common.DependencyInjection;
using Microsoft.Health.Extensions.Fhir;
using Microsoft.Health.Extensions.Fhir.Service;
using Microsoft.Health.FitOnFhir.Authorization;
using Microsoft.Health.FitOnFhir.Common;
using Microsoft.Health.FitOnFhir.Common.Config;
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
using Microsoft.Health.FitOnFhir.GoogleFit.Repositories;
using Microsoft.Health.FitOnFhir.GoogleFit.Services;
using Microsoft.Health.Logging.Telemetry;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Microsoft.Health.FitOnFhir.Authorization
{
    public class Startup : StartupBase
    {
        public override void Configure(IFunctionsHostBuilder builder, IConfiguration configuration)
        {
            EnsureArg.IsNotNull(builder, nameof(builder));

            builder.Services.AddConfiguration<GoogleFitAuthorizationConfiguration>(configuration);
            builder.Services.AddConfiguration<AuthenticationConfiguration>(configuration);
            builder.Services.AddSingleton<IGoogleFitClient, GoogleFitClient>();
            builder.Services.AddSingleton<IGoogleFitDataImporter, GoogleFitDataImporter>();
            builder.Services.AddSingleton<ISecretClientProvider, SecretClientProvider>();
            builder.Services.AddSingleton<IUsersKeyVaultRepository, UsersKeyVaultRepository>();
            builder.Services.AddSingleton<IGoogleFitUserTableRepository, GoogleFitUserTableRepository>();
            builder.Services.AddSingleton<IUsersService, UsersService>();
            builder.Services.AddSingleton<IGoogleFitTokensService, GoogleFitTokensService>();
            builder.Services.AddSingleton<IGoogleFitAuthService, GoogleFitAuthService>();
            builder.Services.AddSingleton<IRoutingService, RoutingService>();
            builder.Services.AddHttpClient<IOpenIdConfigurationProvider, OpenIdConfigurationProvider>();
            builder.Services.AddSingleton<ITokenValidationService, TokenValidationService>();
            builder.Services.AddSingleton<IJwtSecurityTokenHandlerProvider, JwtSecurityTokenHandlerProvider>();
            builder.Services.AddSingleton<IAuthStateService, AuthStateService>();
            builder.Services.AddSingleton<ITelemetryLogger, TelemetryLogger>();
            builder.Services.AddFhirClient(configuration);
            builder.Services.AddSingleton<IFhirService, FhirService>();
            builder.Services.AddSingleton<ResourceManagementService>();
            builder.Services.AddSingleton<GoogleFitAuthorizationHandler>();
            builder.Services.AddSingleton<UnknownAuthorizationHandler>();
            builder.Services.AddSingleton<IQueueService, QueueService>();
            builder.Services.AddSingleton(typeof(Func<DateTimeOffset>), () => DateTimeOffset.UtcNow);
            builder.Services.AddSingleton(sp => sp.CreateOrderedHandlerChain<RoutingRequest, Task<IActionResult>>(typeof(GoogleFitAuthorizationHandler), typeof(UnknownAuthorizationHandler)));
        }
    }
}
