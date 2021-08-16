using System;
using GoogleFitOnFhir.Persistence;
using GoogleFitOnFhir.Repositories;
using GoogleFitOnFhir.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(GoogleFitOnFhir.SyncEvent.Startup))]

namespace GoogleFitOnFhir.SyncEvent
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // TODO: iomtConnectingString from env var or key vault?
            string iomtConnectionString = string.Empty;

            string storageAccountConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            builder.Services.AddSingleton<EventHubContext>(sp => new EventHubContext(iomtConnectionString));
            builder.Services.AddSingleton<StorageAccountContext>(sp => new StorageAccountContext(storageAccountConnectionString));
            builder.Services.AddSingleton<IUsersTableRepository, IUsersTableRepository>();
            builder.Services.AddSingleton<IUsersService, UsersService>();
        }
    }
}