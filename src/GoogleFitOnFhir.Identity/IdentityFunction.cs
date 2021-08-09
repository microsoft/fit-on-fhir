using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        private static readonly string[][] fileArray = new string[][]
        {
            new [] {"api/index.html", "text/html; charset=utf-8"},
            new [] {"api/css/main.css", "text/css; charset=utf-8"},
            new [] {"api/favicon.ico", "image/x-icon"}
        };

        [FunctionName("api")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{p1?}/{p2?}/{p3?}")] HttpRequest req,
            ExecutionContext context,
            ILogger log)
        {
            string root = context.FunctionAppDirectory;
            string path = req.Path.Value.Substring(1);

            // Flatten the user supplied path to its absolute path on the system
            // This will remove relative bits like ../../
            string absPath = Path.GetFullPath(Path.Combine(root, path));

            string[] matchedFile = fileArray.FirstOrDefault(allowedResources =>
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
    }
}
