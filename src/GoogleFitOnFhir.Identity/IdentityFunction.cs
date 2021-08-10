using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2.Web;
using Google.Apis.Fitness.v1;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace GoogleFitOnFhir.Identity
{
    public static class IdentityFunction
    {
        // Whitelisted Files
        private static readonly string[][] FileArray = new string[][]
        {
            new[] { "api/index.html", "text/html; charset=utf-8" },
            new[] { "api/css/main.css", "text/css; charset=utf-8" },
            new[] { "api/img/favicon.ico", "image/x-icon" },
            new[] { "api/img/logo.png", "image/png" },
        };

        [FunctionName("id")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "content/{p1?}/{p2?}/{p3?}")] HttpRequest req,
            Microsoft.Azure.WebJobs.ExecutionContext context,
            ILogger log)
        {
            string requestPath = await new StreamReader(req.Path).ReadToEndAsync();

            string root = context.FunctionAppDirectory;
            string path = requestPath.Substring(1);

            // Flatten the user supplied path to its absolute path on the system
            // This will remove relative bits like ../../
            string absPath = Path.GetFullPath(Path.Combine(root, path));

            var matchedFile = FileArray.FirstOrDefault(allowedResources =>
            {
                // If the flattened path matches the whitelist exactly
                return Path.Combine(root, allowedResources[0]) == absPath;
            });

            if (matchedFile == null)
            {
                return new NotFoundResult();
            }
            else
            {
                // Reconstruct the absPath without using user input at all
                // For maximum safety
                string cleanAbsPath = Path.Combine(root, matchedFile[0]);

                try
                {
                    return new FileStreamResult(new FileStream(cleanAbsPath, FileMode.Open), matchedFile[1]);
                }
                catch (FileNotFoundException err)
                {
                    log.LogError(err.Message);
                    return new NotFoundResult();
                }
            }
        }

        public static async Task<IActionResult> Callback(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "callback")] HttpRequest req,
            Microsoft.Azure.WebJobs.ExecutionContext context)
        {
            FileDataStore fileStore = new FileDataStore(".");
            IAuthorizationCodeFlow flow = GetFlow(fileStore);
            TokenResponse tokenResponse = await fileStore.GetAsync<TokenResponse>("me");

            if (tokenResponse == null)
            {
                string callback = "http" + (req.IsHttps ? "s" : string.Empty) + "://" + Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME") + "/api/callback";

                // Token data does not exist for this user
                tokenResponse = await flow.ExchangeCodeForTokenAsync(
                    "me",
                    req.Query["code"],
                    callback,
                    CancellationToken.None);
            }

            // Contains access and refresh tokens
            UserCredential userCredential = new UserCredential(flow, "me", tokenResponse);

            return new OkObjectResult("auth flow successful");
        }

        [FunctionName("login")]
        public static async Task<IActionResult> Login(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req,
            Microsoft.Azure.WebJobs.ExecutionContext context)
        {
            FileDataStore fileStore = new FileDataStore(".");
            IAuthorizationCodeFlow flow = GetFlow(fileStore);

            StringBuilder stringBuilder = new StringBuilder("http");

            stringBuilder.Append(req.IsHttps ? "s" : string.Empty)
            .Append("://")
            .Append(Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME"))
            .Append("/api/callback");

            var authResult = await new AuthorizationCodeWebApp(flow, stringBuilder.ToString(), string.Empty)
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

        private static IAuthorizationCodeFlow GetFlow(FileDataStore fileStore) =>
            new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                // TODO: Customize datastore to use KeyVault
                // TODO: Securely store and make ClientId/ClientSecret available
                ClientSecrets = new ClientSecrets
                {
                    ClientId = string.Empty,
                    ClientSecret = string.Empty,
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
                DataStore = fileStore,
            });
    }
}
