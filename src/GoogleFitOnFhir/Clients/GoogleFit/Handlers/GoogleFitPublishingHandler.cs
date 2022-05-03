// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using EnsureThat;
using GoogleFitOnFhir.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Common.Handler;

namespace GoogleFitOnFhir.Clients.GoogleFit.Handlers
{
    public class GoogleFitPublishingHandler : IResponsibilityHandler<PublishRequest, Task>
    {
        private readonly ILogger<GoogleFitPublishingHandler> _logger;

        private GoogleFitPublishingHandler()
        {
        }

        public GoogleFitPublishingHandler(ILogger<GoogleFitPublishingHandler> logger)
        {
            _logger = EnsureArg.IsNotNull(logger);
        }

        public static IResponsibilityHandler<PublishRequest, Task> Instance { get; } = new GoogleFitPublishingHandler();

        /// <summary>
        /// Path for callback requests
        /// </summary>
        public static string GoogleFitPlatform => "GoogleFit";

        public Task Evaluate(PublishRequest request)
        {
            try
            {
                if (request.Message.PlatformName == GoogleFitPlatform)
                {
                    // TODO call the new GoogleFitDataImporter here
                    // ImportFitnessData(request.Message.UserId, request.Token)
                    return Task.CompletedTask;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
        }
    }
}
