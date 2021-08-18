using System;
using System.Text;
using Azure.Messaging.EventHubs.Producer;
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
            string iomtConnectionString = Environment.GetEnvironmentVariable("EventHubConnectionString");

            string storageAccountConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            string usersKeyvaultUri = Environment.GetEnvironmentVariable("USERS_KEY_VAULT_URI");

            string googleFitClientId = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_ID");
            string googleFitClientSecret = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_SECRET");

            StringBuilder stringBuilder = new StringBuilder("http");
            #if !DEBUG
            stringBuilder.Append("s");
            #endif
            stringBuilder.Append("://")
                .Append(Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME"))
                .Append("/api/callback");

            builder.Services.AddLogging();
            builder.Services.AddSingleton<GoogleFitClientContext>(sp => new GoogleFitClientContext(googleFitClientId, googleFitClientSecret, stringBuilder.ToString()));
            builder.Services.AddSingleton<GoogleFitClient>();

            builder.Services.AddSingleton<StorageAccountContext>(sp => new StorageAccountContext(storageAccountConnectionString));
            builder.Services.AddSingleton<UsersKeyvaultContext>(sp => new UsersKeyvaultContext(usersKeyvaultUri));
            builder.Services.AddSingleton<EventHubProducerClient>(sp => new EventHubProducerClient(iomtConnectionString));

            builder.Services.AddSingleton<IUsersKeyvaultRepository, UsersKeyvaultRepository>();
            builder.Services.AddSingleton<IAuthService, AuthService>();
            builder.Services.AddSingleton<IUsersTableRepository, UsersTableRepository>();
            builder.Services.AddSingleton<IUsersService, UsersService>();
        }
    }
}
