using System;
using GoogleFitOnFhir.Persistence;
using GoogleFitOnFhir.Repositories;
using GoogleFitOnFhir.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(GoogleFitOnFhir.Identity.Startup))]

namespace GoogleFitOnFhir.Identity
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddLogging();

            // TODO: iomtConnectingString from env var or key vault?
            string iomtConnectionString = string.Empty;
            string storageAccountConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            builder.Services.AddSingleton<EventHubContext>(sp => new EventHubContext(iomtConnectionString));
            builder.Services.AddSingleton<StorageAccountContext>(sp => new StorageAccountContext(storageAccountConnectionString));
            builder.Services.AddSingleton<IUsersTableRepository, UsersTableRepository>();
            builder.Services.AddSingleton<IUsersService, UsersService>();

            // builder.Services.BuildServiceProvider(true);
        }
    }
}