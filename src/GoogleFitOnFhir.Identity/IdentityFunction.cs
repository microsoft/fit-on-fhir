using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2.Web;
using Google.Apis.Fitness.v1;
using Google.Apis.PeopleService.v1.Data;
using GoogleFitOnFhir.Models;
using GoogleFitOnFhir.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace GoogleFitOnFhir.Identity
{
    public class IdentityFunction
    {
        private readonly IUsersService usersService;

        private readonly ILogger log;

        // Whitelisted Files
        private readonly string[][] fileMap = new string[][]
        {
            new[] { "api/index.html", "text/html; charset=utf-8" },
            new[] { "api/css/main.css", "text/css; charset=utf-8" },
            new[] { "api/img/favicon.ico", "image/x-icon" },
            new[] { "api/img/logo.png", "image/png" },
        };

        public IdentityFunction(IUsersService usersService, ILogger<IdentityFunction> log)
        {
            this.usersService = usersService;
            this.log = log;
        }

        [FunctionName("api")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{p1?}/{p2?}/{p3?}")] HttpRequest req,
            Microsoft.Azure.WebJobs.ExecutionContext context)
        {
            string root = context.FunctionAppDirectory;
            string path = req.Path.Value[1..];

            if (path.StartsWith("api/login"))
            {
                return await this.Login(req, this.log);
            }
            else if (path.StartsWith("api/callback"))
            {
                return await this.Callback(req, this.usersService, this.log);
            }

            // Flatten the user supplied path to it's absolute path on the system
            // This will remove relative bits like ../../
            var absPath = Path.GetFullPath(Path.Combine(root, path));

            var matchedFile = this.fileMap.FirstOrDefault(allowedResources =>
            {
                // If the flattened path matches the whitelist exactly
                return Path.Combine(root, allowedResources[0]) == absPath;
            });

            if (matchedFile != null)
            {
                // Reconstruct the absPath without using user input at all
                // For maximum safety
                var cleanAbsPath = Path.Combine(root, matchedFile[0]);
                return this.FileStreamOrNotFound(cleanAbsPath, matchedFile[1]);
            }

            // Return the first item in the FileMap by default
            var firstFile = this.fileMap.First();
            var firstFilePath = Path.Combine(root, firstFile[0]);
            return this.FileStreamOrNotFound(firstFilePath, firstFile[1]);
        }

        public async Task<IActionResult> Callback(HttpRequest req, IUsersService usersService, ILogger log)
        {
            IAuthorizationCodeFlow flow = this.GetFlow();
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
                var user = new User(base58Email);
                usersService.Initiate(user);
            }

            return new OkObjectResult("auth flow successful");
        }

        public async Task<IActionResult> Login(HttpRequest req, ILogger log)
        {
            IAuthorizationCodeFlow flow = this.GetFlow();
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

        private IActionResult FileStreamOrNotFound(string filePath, string contentType)
        {
            return File.Exists(filePath) ?
                (IActionResult)new FileStreamResult(File.OpenRead(filePath), contentType) :
                new NotFoundResult();
        }

        private IAuthorizationCodeFlow GetFlow()
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
    }
}
