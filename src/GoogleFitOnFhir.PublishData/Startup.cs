using System;
using GoogleFitOnFhir.Persistence;
using GoogleFitOnFhir.Repositories;
using GoogleFitOnFhir.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

using GoogleFitClient = GoogleFitOnFhir.Clients.GoogleFit.Client;
using GoogleFitClientContext = GoogleFitOnFhir.Clients.GoogleFit.ClientContext;

[assembly: FunctionsStartup(typeof(GoogleFitOnFhir.PublishData.Startup))]

namespace GoogleFitOnFhir.PublishData
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // TODO: iomtConnectingString from env var or key vault?
            string iomtConnectionString = string.Empty;

            string storageAccountConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            string googleFitClientId = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_ID");
            string googleFitClientSecret = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_SECRET");

            #if DEBUG
            string googleFitCallbackUri = "http://" + Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME") + "/api/callback";
            #else
            string googleFitCallbackUri = "https://" + Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME") + "/api/callback";
            #endif

            builder.Services.AddSingleton<GoogleFitClientContext>(sp => new GoogleFitClientContext(googleFitClientId, googleFitClientSecret, googleFitCallbackUri));
            builder.Services.AddSingleton<GoogleFitClient>();

            builder.Services.AddSingleton<EventHubContext>(sp => new EventHubContext(iomtConnectionString));
            builder.Services.AddSingleton<StorageAccountContext>(sp => new StorageAccountContext(storageAccountConnectionString));
            builder.Services.AddSingleton<IUsersTableRepository, UsersTableRepository>();
            builder.Services.AddSingleton<IUsersService, UsersService>();
        }
    }
}
