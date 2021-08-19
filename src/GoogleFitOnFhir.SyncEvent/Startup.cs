using System;
using GoogleFitOnFhir.Persistence;
using GoogleFitOnFhir.Repositories;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

using GoogleFitClient = GoogleFitOnFhir.Clients.GoogleFit.Client;
using GoogleFitClientContext = GoogleFitOnFhir.Clients.GoogleFit.ClientContext;

[assembly: FunctionsStartup(typeof(GoogleFitOnFhir.SyncEvent.Startup))]

namespace GoogleFitOnFhir.SyncEvent
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            string storageAccountConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            builder.Services.AddSingleton<StorageAccountContext>(sp => new StorageAccountContext(storageAccountConnectionString));

            builder.Services.AddSingleton<IUsersTableRepository, UsersTableRepository>();
        }
    }
}