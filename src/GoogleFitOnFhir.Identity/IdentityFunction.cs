// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly IUsersService _usersService;
        private readonly IAuthService _authService;
        private readonly ILogger _logger;

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
            ILogger<IdentityFunction> logger)
        {
            _usersService = usersService;
            _authService = authService;
            _logger = logger;
        }

        [FunctionName("api")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{p1?}/{p2?}/{p3?}")] HttpRequest req,
            Microsoft.Azure.WebJobs.ExecutionContext context,
            CancellationToken cancellationToken)
        {
            using (var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, req.HttpContext.RequestAborted))
            {
                string root = context.FunctionAppDirectory;
                string path = req.Path.Value[1..];

                if (path.StartsWith("api/login"))
                {
                    return await Login(req, cancellationSource.Token);
                }
                else if (path.StartsWith("api/callback"))
                {
                    return await Callback(req, cancellationSource.Token);
                }

                // Flatten the user supplied path to it's absolute path on the system
                // This will remove relative bits like ../../
                var absPath = Path.GetFullPath(Path.Combine(root, path));

                var matchedFile = fileArray.FirstOrDefault(allowedResources =>
                {
                    // If the flattened path matches the Allow-listed file exactly
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
                var firstFile = fileArray.First();
                var firstFilePath = Path.Combine(root, firstFile[0]);
                return FileStreamOrNotFound(firstFilePath, firstFile[1]);
            }
        }

        public async Task<IActionResult> Callback(HttpRequest req, CancellationToken cancellationToken)
        {
            try
            {
                await _usersService.Initiate(req.Query["code"], cancellationToken);
                return new OkObjectResult("auth flow successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new NotFoundObjectResult(ex.Message);
            }
        }

        public async Task<IActionResult> Login(HttpRequest req, CancellationToken cancellationToken)
        {
            AuthUriResponse response = await _authService.AuthUriRequest(cancellationToken);
            return new RedirectResult(response.Uri);
        }

        private IActionResult FileStreamOrNotFound(string filePath, string contentType)
        {
            return File.Exists(filePath) ?
                new FileStreamResult(File.OpenRead(filePath), contentType) :
                new NotFoundResult();
        }
    }
}
