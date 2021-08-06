using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;

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
            ExecutionContext context,
            ILogger log)
        {
            string root = context.FunctionAppDirectory;
            string path = req.Path.Value.Substring(1);

            // Flatten the user supplied path to it's absolute path on the system
            // This will remove relative bits like ../../
            var absPath = Path.GetFullPath(Path.Combine(root, path));
            
            var matchedFile = FileMap.FirstOrDefault((allowedResources => {
                // If the flattened path matches the whitelist exactly
                return Path.Combine(root, allowedResources[0]) == absPath;
            }));

            if(matchedFile != null)
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

        private static IActionResult fileStreamOrNotFound(string filePath, string contentType)
        {
            return File.Exists(filePath) ? 
                (IActionResult)new FileStreamResult(File.OpenRead(filePath), contentType) : 
                new NotFoundResult();
        }
    }
}
