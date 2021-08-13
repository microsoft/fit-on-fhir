﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2.Web;
using Google.Apis.Fitness.v1;
using Google.Apis.PeopleService.v1.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace GoogleFitOnFhir.Identity
{
    public static class IdentityFunction
    {
        // Whitelisted Files
        private static readonly string[][] FileMap = new string[][]
        {
            new[] { "api/index.html", "text/html; charset=utf-8" },
            new[] { "api/css/main.css", "text/css; charset=utf-8" },
            new[] { "api/img/favicon.ico", "image/x-icon" },
            new[] { "api/img/logo.png", "image/png" },
        };

        [FunctionName("api")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{p1?}/{p2?}/{p3?}")] HttpRequest req,
            Microsoft.Azure.WebJobs.ExecutionContext context,
            ILogger log)
        {
            string root = context.FunctionAppDirectory;
            string path = req.Path.Value[1..];

            if (path.StartsWith("api/login"))
            {
                return await Login(req, log);
            }
            else if (path.StartsWith("api/callback"))
            {
                return await Callback(req, log);
            }

            // Flatten the user supplied path to it's absolute path on the system
            // This will remove relative bits like ../../
            var absPath = Path.GetFullPath(Path.Combine(root, path));

            var matchedFile = FileMap.FirstOrDefault(allowedResources =>
            {
                // If the flattened path matches the whitelist exactly
                return Path.Combine(root, allowedResources[0]) == absPath;
            });

            if (matchedFile != null)
            {
                // Reconstruct the absPath without using user input at all
                // For maximum safety
                var cleanAbsPath = Path.Combine(root, matchedFile[0]);
                return FileStreamOrNotFound(cleanAbsPath, matchedFile[1]);
            }

            // Return the first item in the FileMap by default
            var firstFile = FileMap.First();
            var firstFilePath = Path.Combine(root, firstFile[0]);
            return FileStreamOrNotFound(firstFilePath, firstFile[1]);
        }

        public static async Task<IActionResult> Callback(HttpRequest req, ILogger log)
        {
            IAuthorizationCodeFlow flow = GetFlow();
            string callback = "http" + (req.IsHttps ? "s" : string.Empty) + "://" + Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME") + "/api/callback";
            TokenResponse tokenResponse = await flow.ExchangeCodeForTokenAsync(
                "me",
                req.Query["code"],
                callback,
                CancellationToken.None);

            if (tokenResponse != null && tokenResponse.RefreshToken != null)
            {
                UserCredential userCredential = new UserCredential(flow, "me", tokenResponse);
                GoogleFitData googleFitData = new GoogleFitData(tokenResponse.AccessToken);
                Person me = googleFitData.GetMyInfo();
                string base58Email = GoogleFitOnFhir.Utility.Base58String(me.EmailAddresses[0].Value);

                // Write refreshToken to Key Vault with base58 of email as secret name
                AzureServiceTokenProvider azureServiceTokenProvider1 = new AzureServiceTokenProvider();
                KeyVaultClient kvClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider1.KeyVaultTokenCallback));
                await kvClient.SetSecretAsync(Environment.GetEnvironmentVariable("USERS_KEY_VAULT_URI"), base58Email, tokenResponse.RefreshToken);

                // Use base58Email as UserId and update the UsersTable
                UpdateUserId(base58Email, log);
            }

            return new OkObjectResult("auth flow successful");
        }

        public static async Task<IActionResult> Login(HttpRequest req, ILogger log)
        {
            IAuthorizationCodeFlow flow = GetFlow();
            string callback = "http" + (req.IsHttps ? "s" : string.Empty) + "://" + Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME") + "/api/callback";
            var authResult = await new AuthorizationCodeWebApp(flow, callback, string.Empty)
                .AuthorizeAsync("user", CancellationToken.None);

            if (authResult.Credential == null)
            {
                return new RedirectResult(authResult.RedirectUri);
            }
            else
            {
                // Not sure when this would happen
                return new OkObjectResult("already authed");
            }
        }

        private static IActionResult FileStreamOrNotFound(string filePath, string contentType)
        {
            return File.Exists(filePath) ?
                (IActionResult)new FileStreamResult(File.OpenRead(filePath), contentType) :
                new NotFoundResult();
        }

        private static IAuthorizationCodeFlow GetFlow()
        {
            // TODO: Customize datastore to use KeyVault
            return new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                // TODO: Securely store and make ClientId/ClientSecret available
                ClientSecrets = new ClientSecrets
                {
                    ClientId = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_ID"),
                    ClientSecret = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_SECRET"),
                },

                // TODO: Only need write scopes for e2e tests - make this dynamic
                Scopes = new[]
                {
                    "https://www.googleapis.com/auth/userinfo.email",
                    "https://www.googleapis.com/auth/userinfo.profile",
                    FitnessService.Scope.FitnessBloodGlucoseRead,
                    FitnessService.Scope.FitnessBloodGlucoseWrite,
                    FitnessService.Scope.FitnessHeartRateRead,
                    FitnessService.Scope.FitnessHeartRateWrite,
                },
            });
        }

        private static bool UpdateUserId(string userId, ILogger log)
        {
            string storageAccountConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            TableClient tableClient = new TableClient(storageAccountConnectionString, "users");

            UserRecord user = new UserRecord(userId);

            try
            {
                tableClient.UpsertEntity(user);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                return false;
            }

            return true;
        }
    }
}
