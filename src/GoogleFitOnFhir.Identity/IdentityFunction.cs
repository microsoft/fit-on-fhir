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
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Http;
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
        private static readonly string[][] FileMap = new string[][]
        {
            new [] {"api/index.html",   "text/html; charset=utf-8"},
            new [] {"api/css/main.css", "text/css; charset=utf-8"},
            new [] {"api/favicon.ico",  "image/x-icon"}
        };

        [FunctionName("api")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{p1?}/{p2?}/{p3?}")] HttpRequest req,
            Microsoft.Azure.WebJobs.ExecutionContext context,
            ILogger log)
        {
            string root = context.FunctionAppDirectory;
            string path = req.Path.Value.Substring(1);

            if (path.StartsWith("api/login"))
            {
                return await Task.Run(() =>
                {
                    return Login(req, context, log);
                });
            }
            else if (path.StartsWith("api/callback"))
            {
                return await Task.Run(() =>
                {
                    return Callback(req, context, log);
                });
            }

            // Flatten the user supplied path to it's absolute path on the system
            // This will remove relative bits like ../../
            var absPath = Path.GetFullPath(Path.Combine(root, path));

            var matchedFile = FileMap.FirstOrDefault((allowedResources =>
            {
                // If the flattened path matches the whitelist exactly
                return Path.Combine(root, allowedResources[0]) == absPath;
            }));

            if (matchedFile != null)
            {
                // Reconstruct the absPath without using user input at all
                // For maximum safety
                var cleanAbsPath = Path.Combine(root, matchedFile[0]);
                return fileStreamOrNotFound(cleanAbsPath, matchedFile[1]);
            }

            // Return the first item in the FileMap by default
            var firstFile = FileMap.First();
            var firstFilePath = Path.Combine(root, firstFile[0]);
            return fileStreamOrNotFound(firstFilePath, firstFile[1]);
        }

        public static async Task<IActionResult> Callback(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{p1?}/{p2?}/{p3?}")] HttpRequest req,
            Microsoft.Azure.WebJobs.ExecutionContext context,
            ILogger log
        )
        {
            FileDataStore fileStore = new FileDataStore(".");
            IAuthorizationCodeFlow flow = GetFlow(fileStore);
            TokenResponse tokenResponse = await fileStore.GetAsync<TokenResponse>("me");

            if (tokenResponse == null)
            {
                string callback = "http" + (req.IsHttps ? "s" : "") + "://" + Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME") + "/api/callback";
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

        public static async Task<IActionResult> Login(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{p1?}/{p2?}/{p3?}")] HttpRequest req,
            Microsoft.Azure.WebJobs.ExecutionContext context,
            ILogger log
        )
        {
            FileDataStore fileStore = new FileDataStore(".");
            IAuthorizationCodeFlow flow = GetFlow(fileStore);

            string callback = "http" + (req.IsHttps ? "s" : "") + "://" + Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME") + "/api/callback";
            var authResult = await new AuthorizationCodeWebApp(flow, callback, "")
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

        private static IActionResult fileStreamOrNotFound(string filePath, string contentType)
        {
            return File.Exists(filePath) ?
                (IActionResult)new FileStreamResult(File.OpenRead(filePath), contentType) :
                new NotFoundResult();
        }

        private static IAuthorizationCodeFlow GetFlow(FileDataStore fileStore)
        {
            // TODO: Customize datastore to use KeyVault
            return new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                // TODO: Securely store and make ClientId/ClientSecret available
                ClientSecrets = new ClientSecrets
                {
                    ClientId = "",
                    ClientSecret = ""
                },
                // TODO: Only need write scopes for e2e tests - make this dynamic
                Scopes = new[] {
                    "https://www.googleapis.com/auth/userinfo.email",
                    "https://www.googleapis.com/auth/userinfo.profile",
                    FitnessService.Scope.FitnessBloodGlucoseRead,
                    FitnessService.Scope.FitnessBloodGlucoseWrite,
                    FitnessService.Scope.FitnessHeartRateRead,
                    FitnessService.Scope.FitnessHeartRateWrite,

                },
                DataStore = fileStore
            });
        }
    }
}
