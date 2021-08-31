using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Web;
using Google.Apis.Fitness.v1;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;
using GoogleFitOnFhir.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace GoogleFitOnFhir.Identity
{
    public class IdentityFunction
    {
        private readonly IUsersService usersService;
        private readonly IAuthService authService;
        private readonly ILogger log;

        // Allow-listed Files
        private readonly string[][] fileArray = new string[][]
        {
            new[] { "api/index.html", "text/html; charset=utf-8" },
            new[] { "api/css/main.css", "text/css; charset=utf-8" },
            new[] { "api/img/favicon.ico", "image/x-icon" },
            new[] { "api/img/logo.png", "image/png" },
        };

        public IdentityFunction(
            IUsersService usersService,
            IAuthService authService,
            ILogger<IdentityFunction> log)
        {
            this.usersService = usersService;
            this.authService = authService;
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
                return await this.Login(req);
            }
            else if (path.StartsWith("api/callback"))
            {
                return await this.Callback(req);
            }

            // Flatten the user supplied path to it's absolute path on the system
            // This will remove relative bits like ../../
            var absPath = Path.GetFullPath(Path.Combine(root, path));

            var matchedFile = this.fileArray.FirstOrDefault(allowedResources =>
            {
                // If the flattened path matches the Allow-listed file exactly
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
            var firstFile = this.fileArray.First();
            var firstFilePath = Path.Combine(root, firstFile[0]);
            return this.FileStreamOrNotFound(firstFilePath, firstFile[1]);
        }

        public async Task<IActionResult> Callback(HttpRequest req)
        {
            try
            {
                await this.usersService.Initiate(req.Query["code"]);
                return new OkObjectResult("auth flow successful");
            }
            catch (Exception ex)
            {
                this.log.LogError(ex.Message);
                return new NotFoundObjectResult("Unable to authorize");
            }
        }

        public async Task<IActionResult> Login(HttpRequest req)
        {
            AuthUriResponse response = await this.authService.AuthUriRequest();
            return new RedirectResult(response.Uri);
        }

        private IActionResult FileStreamOrNotFound(string filePath, string contentType)
        {
            return File.Exists(filePath) ?
                (IActionResult)new FileStreamResult(File.OpenRead(filePath), contentType) :
                new NotFoundResult();
        }
    }
}
