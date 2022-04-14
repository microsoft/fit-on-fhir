// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnsureThat;
using GoogleFitOnFhir.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Common.Handler;

namespace GoogleFitOnFhir.Clients.GoogleFit.Handlers
{
    public class GoogleFitFileHandler : IResponsibilityHandler<(IServiceScope scope, RoutingRequest request), Task<IActionResult>>
    {
        // Allow-listed Files
        private readonly string[][] _fileArray = new string[][]
        {
            new[] { "api/index.html", "text/html; charset=utf-8" },
            new[] { "api/css/main.css", "text/css; charset=utf-8" },
            new[] { "api/img/favicon.ico", "image/x-icon" },
            new[] { "api/img/logo.png", "image/png" },
        };

        private GoogleFitFileHandler()
        {
        }

        public static IResponsibilityHandler<(IServiceScope scope, RoutingRequest request), Task<IActionResult>> Instance { get; } = new GoogleFitFileHandler();

        public Task<IActionResult> Evaluate((IServiceScope scope, RoutingRequest request) operation)
        {
            var path = EnsureArg.IsNotNull(operation.request.HttpRequest.Path.Value?[1..]);
            var root = operation.request.Root;

            // Flatten the user supplied path to it's absolute path on the system
            // This will remove relative bits like ../../
            var absPath = Path.GetFullPath(Path.Combine(root, path));

            var matchedFile = _fileArray.FirstOrDefault(allowedResources => Path.Combine(root, allowedResources[0]) == absPath);

            if (matchedFile != null)
            {
                // Reconstruct the absPath without using user input at all
                // For maximum safety
                var cleanAbsPath = Path.Combine(root, matchedFile[0]);
                return FileStreamOrNotFound(cleanAbsPath, matchedFile[1]);
            }

            // Return the first item in the FileMap by default
            var firstFile = _fileArray.First();
            var firstFilePath = Path.Combine(root, firstFile[0]);
            return FileStreamOrNotFound(firstFilePath, firstFile[1]);
        }

        private Task<IActionResult> FileStreamOrNotFound(string filePath, string contentType)
        {
            return Task.Run<IActionResult>(() => File.Exists(filePath)
                ? new FileStreamResult(File.OpenRead(filePath), contentType)
                : new NotFoundResult());
        }
    }
}
