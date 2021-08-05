using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GoogleFitOnFhir.Identity
{
    public static class IdentityFunction
    {
        // Whitelisted Files
        private static readonly string[][] FileMap = new string[][]
        {
            new [] {'/public/index.html',   'text/html; charset=utf-8'},
            new [] {'/public/css/main.css', 'text/css; charset=utf-8'},
            new [] {'/public/favicon.ico',  'image/x-icon'}            
        };

        [FunctionName("Identity")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string root = context.FunctionAppDirectory;
            string path = 'public/' + req.Path.Value;
            
            var contentType = FileMap.FirstOrDefault((kv => path.StartsWith(kv[0])));
            
            // Flatten the user supplied path to it's absolute path on the system
            // This will remove relative bits like ../../
            var absPath = IO.Path.GetFullPath(IO.Path.Combine(root, absPath));
            
            var matchedFile = FileMap.FirstOrDefault((allowedResources => {
                // If the flattened path matches the whitelist exactly
                return IO.Path.Combine(root, allowedResources[0]) == absPath;
            }));

            if(matchedFile != null)
            {
                // Reconstruct the absPath without using user input at all
                // For maximum safety
                var cleanAbsPath = IO.Path.Combine(root, matchedFile[0]);
                return fileStreamOrNotFound(cleanAbsPath, matchedFile[1]);
            }

            // Return the first item in the FileMap by default
            var firstFile = FileMap.First();
            var firstFilePath = IO.Path.Combine(firstFile[0], firstFile[1]);
            return fileStreamOrNotFound(firstFilePath, firstFile[1]);
        }

        private static IActionResult fileStreamOrNotFound(filePath, contentType)
        {
            return File.Exists(filePath) ? 
                (IActionResult)new FileStreamResult(File.OpenRead(filePath), contentType) : 
                new NotFoundResult();
        }
    }
}
