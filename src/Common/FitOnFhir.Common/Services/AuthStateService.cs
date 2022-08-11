﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text;
using System.Web;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Health.FitOnFhir.Common.Config;
using Microsoft.Health.FitOnFhir.Common.Exceptions;
using Microsoft.Health.FitOnFhir.Common.ExtensionMethods;
using Microsoft.Health.FitOnFhir.Common.Interfaces;
using Microsoft.Health.FitOnFhir.Common.Models;
using Newtonsoft.Json;

namespace Microsoft.Health.FitOnFhir.Common.Services
{
    public class AuthStateService : IAuthStateService
    {
        private string _base36Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private Random _random = new Random();
        private BlobContainerClient _blobContainerClient;
        private BlobServiceClient _blobServiceClient;
        private readonly AuthenticationConfiguration _authenticationConfiguration;
        private readonly IJwtSecurityTokenHandlerProvider _jwtSecurityTokenHandlerProvider;
        private readonly ILogger _logger;
        private readonly Func<DateTimeOffset> _utcNowFunc;

        public AuthStateService(
            AzureConfiguration azureConfiguration,
            AuthenticationConfiguration authenticationConfiguration,
            IJwtSecurityTokenHandlerProvider jwtSecurityTokenHandlerProvider,
            BlobServiceClient blobServiceClient,
            Func<DateTimeOffset> utcNowFunc,
            ILogger<AuthStateService> logger)
        {
            if (blobServiceClient == null)
            {
                var connectionString = EnsureArg.IsNotNullOrWhiteSpace(
                    azureConfiguration.StorageAccountConnectionString,
                    nameof(azureConfiguration.StorageAccountConnectionString));
                _blobServiceClient = new BlobServiceClient(connectionString);
            }
            else
            {
                _blobServiceClient = blobServiceClient;
            }

            _blobContainerClient = _blobServiceClient.GetBlobContainerClient(azureConfiguration.BlobContainerName);
            _authenticationConfiguration = EnsureArg.IsNotNull(authenticationConfiguration, nameof(authenticationConfiguration));
            _jwtSecurityTokenHandlerProvider = EnsureArg.IsNotNull(jwtSecurityTokenHandlerProvider, nameof(jwtSecurityTokenHandlerProvider));
            _utcNowFunc = EnsureArg.IsNotNull(utcNowFunc);
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        public string ExternalIdentifier { get; set; }

        public string ExternalSystem { get; set; }

        public string RedirectUrl { get; set; }

        public string State { get; set; }

        /// <inheritdoc/>
        public AuthState CreateAuthState(HttpRequest httpRequest)
        {
            var externalId = HttpUtility.UrlDecode(httpRequest.Query[Constants.ExternalIdQueryParameter]);

            var externalSystem = HttpUtility.UrlDecode(httpRequest.Query[Constants.ExternalSystemQueryParameter]);

            // is this for anonymous logins?
            if (_authenticationConfiguration.IsAnonymousLoginEnabled)
            {
                ExternalIdentifier = EnsureArg.IsNotNullOrWhiteSpace(externalId, nameof(externalId));
                ExternalSystem = EnsureArg.IsNotNullOrWhiteSpace(externalSystem, nameof(externalSystem));
            }
            else
            {
                // do not allow the ExternalId or ExternalSystem query params when authentication is enabled
                if (!string.IsNullOrEmpty(externalId) || !string.IsNullOrEmpty(externalSystem))
                {
                    string errorMessage = $"{Constants.ExternalIdQueryParameter} and {Constants.ExternalSystemQueryParameter} are forbidden query parameters with non-anonymous authorization.";
                    _logger.LogError(errorMessage);
                    throw new ArgumentException(errorMessage);
                }

                // extract the token from the header.
                if (!httpRequest.TryGetTokenStringFromAuthorizationHeader(JwtBearerDefaults.AuthenticationScheme, out string token))
                {
                    _logger.LogError("The request Authorization header is invalid.");
                    return default;
                }

                // Read the token
                _jwtSecurityTokenHandlerProvider.SetMapInboundClaims(false);
                var jwtSecurityToken = _jwtSecurityTokenHandlerProvider.ReadJwtToken(token);

                if (jwtSecurityToken == default)
                {
                    return default;
                }

                ExternalIdentifier = jwtSecurityToken.Subject;
                ExternalSystem = jwtSecurityToken.Issuer;
            }

            var redirectUrl = HttpUtility.UrlDecode(httpRequest.Query[Constants.RedirectUrlQueryParameter]);

            if (string.IsNullOrEmpty(redirectUrl))
            {
                string errorMessage = $"The required parameter {Constants.RedirectUrlQueryParameter} was not provided in request";
                _logger.LogError(errorMessage);
                throw new RedirectUrlException(errorMessage);
            }

            if (!_authenticationConfiguration.ApprovedRedirectUrls.Any(url => string.Equals(url, redirectUrl, StringComparison.OrdinalIgnoreCase)))
            {
                string errorMessage = "The redirect URL was not found in the list of approved redirect URLs.";
                _logger.LogError(errorMessage);
                throw new RedirectUrlException(errorMessage);
            }

            RedirectUrl = redirectUrl;
            State = HttpUtility.UrlDecode(httpRequest.Query[Constants.StateQueryParameter]);

            return new AuthState(
                ExternalIdentifier,
                ExternalSystem,
                _utcNowFunc() + Constants.AuthStateExpiry,
                RedirectUrl,
                State);
        }

        public async Task<AuthState> RetrieveAuthState(string nonce, CancellationToken cancellationToken)
        {
            try
            {
                var blobName = EnsureArg.IsNotNullOrWhiteSpace(nonce);

                // Get a reference to the blob
                BlobClient blobClient = _blobContainerClient.GetBlobClient(blobName);

                // Get the AuthState
                var response = await blobClient.DownloadContentAsync(cancellationToken);

                // deserialize the AuthState
                StreamReader reader = new StreamReader(response.Value.Content.ToStream());
                string json = reader.ReadToEnd();
                return AuthState.Parse(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve the auth state.");
                throw;
            }
        }

        public async Task<string> StoreAuthState(AuthState state, CancellationToken cancellationToken)
        {
            try
            {
                var nonce = GenerateNonce(Constants.NonceLength);

                // Get a reference to the blob
                BlobClient blobClient = _blobContainerClient.GetBlobClient(nonce);

                // store state to blob storage
                await blobClient.UploadAsync(new BinaryData(JsonConvert.SerializeObject(state)), true, cancellationToken);

                return nonce;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store the auth state.");
                return null;
            }
        }

        private string GenerateNonce(int length)
        {
            var nonce = new StringBuilder();
            for (var i = 0; i < length; i++)
            {
                nonce.Append(_base36Chars[_random.Next(0, _base36Chars.Length - 1)]);
            }

            return nonce.ToString();
        }
    }
}
