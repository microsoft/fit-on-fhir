// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using EnsureThat;
using GoogleFitOnFhir.Common;
using GoogleFitOnFhir.Models;
using GoogleFitOnFhir.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Common.Handler;

namespace GoogleFitOnFhir.Clients.GoogleFit.Handlers
{
    public class GoogleFitPublishingHandler : IResponsibilityHandler<PublishRequest, Task>
    {
        private readonly IGoogleFitDataImporter _googleFitDataImporter;
        private readonly IErrorHandler _errorHandler;
        private readonly ILogger<GoogleFitPublishingHandler> _logger;

        private GoogleFitPublishingHandler()
        {
        }

        public GoogleFitPublishingHandler(
            IGoogleFitDataImporter googleFitDataImporter,
            IErrorHandler errorHandler,
            ILogger<GoogleFitPublishingHandler> logger)
        {
            _googleFitDataImporter = EnsureArg.IsNotNull(googleFitDataImporter);
            _errorHandler = EnsureArg.IsNotNull(errorHandler);
            _logger = EnsureArg.IsNotNull(logger);
        }

        public static IResponsibilityHandler<PublishRequest, Task> Instance { get; } = new GoogleFitPublishingHandler();

        /// <summary>
        /// String identifier for the GoogleFit platform.  Used to help identify the recipient platform for a <see cref="QueueMessage"/>.
        /// </summary>
        public static string GoogleFitPlatform => "GoogleFit";

        public Task Evaluate(PublishRequest request)
        {
            try
            {
                if (request.Message.PlatformName == GoogleFitPlatform)
                {
                    _googleFitDataImporter.Import(request.Message.UserId, request.Token);
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
                _errorHandler.HandleDataSyncError(request.Message, ex);
                return null;
            }
        }
    }
}
