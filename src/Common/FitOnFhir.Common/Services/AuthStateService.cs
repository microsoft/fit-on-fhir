// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;
using System.Web;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Health.FitOnFhir.Common.Config;
using Microsoft.Health.FitOnFhir.Common.Exceptions;
using Microsoft.Health.FitOnFhir.Common.ExtensionMethods;
using Microsoft.Health.FitOnFhir.Common.Interfaces;
using Microsoft.Health.FitOnFhir.Common.Models;
using Microsoft.Health.FitOnFhir.Common.Providers;
using Newtonsoft.Json;

namespace Microsoft.Health.FitOnFhir.Common.Services
{
    public class AuthStateService : IAuthStateService
    {
        private string _base36Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private BlobContainerClient _blobContainerClient;
        private readonly AuthenticationConfiguration _authenticationConfiguration;
        private readonly IJwtSecurityTokenHandlerProvider _jwtSecurityTokenHandlerProvider;
        private readonly Func<DateTimeOffset> _utcNowFunc;

        public AuthStateService(
            AuthenticationConfiguration authenticationConfiguration,
            IBlobContainerClientProvider blobContainerClientProvider,
            IJwtSecurityTokenHandlerProvider jwtSecurityTokenHandlerProvider,
            Func<DateTimeOffset> utcNowFunc)
        {
            _blobContainerClient = EnsureArg.IsNotNull(blobContainerClientProvider, nameof(blobContainerClientProvider)).GetBlobContainerClient(Constants.AuthDataBlobContainerName);
            _authenticationConfiguration = EnsureArg.IsNotNull(authenticationConfiguration, nameof(authenticationConfiguration));
            _jwtSecurityTokenHandlerProvider = EnsureArg.IsNotNull(jwtSecurityTokenHandlerProvider, nameof(jwtSecurityTokenHandlerProvider));
            _utcNowFunc = EnsureArg.IsNotNull(utcNowFunc);
        }

        /// <inheritdoc/>
        public AuthState CreateAuthState(HttpRequest httpRequest)
        {
            List<string> errorParams = new List<string>();

            var request = EnsureArg.IsNotNull(httpRequest);

            var externalId = HttpUtility.UrlDecode(request.Query[Constants.ExternalIdQueryParameter]);
            var externalSystem = HttpUtility.UrlDecode(request.Query[Constants.ExternalSystemQueryParameter]);
            var redirectUrl = HttpUtility.UrlDecode(request.Query[Constants.RedirectUrlQueryParameter]);

            // is this for anonymous logins?
            if (_authenticationConfiguration.IsAnonymousLoginEnabled)
            {
                if (string.IsNullOrWhiteSpace(externalId))
                {
                    errorParams.Add($"{Constants.ExternalIdQueryParameter}");
                }

                if (string.IsNullOrWhiteSpace(externalSystem))
                {
                    errorParams.Add($"{Constants.ExternalSystemQueryParameter}");
                }

                if (string.IsNullOrWhiteSpace(redirectUrl))
                {
                    errorParams.Add($"{Constants.RedirectUrlQueryParameter}");
                }

                if (errorParams.Any())
                {
                    string errorMessageFormat = "{0}, {1}, {2} are required query parameters with anonymous authorization. The request is missing {3}.";
                    string missing = string.Join(", ", errorParams);
                    string message = string.Format(errorMessageFormat, Constants.ExternalIdQueryParameter, Constants.ExternalSystemQueryParameter, Constants.RedirectUrlQueryParameter, missing);
                    throw new AuthStateException(message);
                }
            }
            else
            {
                // do not allow the ExternalId or ExternalSystem query params when authentication is enabled
                if (!string.IsNullOrWhiteSpace(externalId))
                {
                    errorParams.Add($"{Constants.ExternalIdQueryParameter}");
                }

                if (!string.IsNullOrWhiteSpace(externalSystem))
                {
                    errorParams.Add($"{Constants.ExternalSystemQueryParameter}");
                }

                if (errorParams.Any())
                {
                    string errorMessageFormat = "{0} and {1} are forbidden query parameters with authenticated authorization. The request contains {2}.";
                    string present = string.Join(", ", errorParams);
                    string message = string.Format(errorMessageFormat, Constants.ExternalIdQueryParameter, Constants.ExternalSystemQueryParameter, present);

                    throw new AuthStateException(message);
                }

                if (string.IsNullOrEmpty(redirectUrl))
                {
                    throw new AuthStateException($"The required parameter {Constants.RedirectUrlQueryParameter} was not provided in the request.");
                }

                // extract the token from the header.
                if (!request.TryGetTokenStringFromAuthorizationHeader(JwtBearerDefaults.AuthenticationScheme, out string token))
                {
                    throw new AuthStateException("The request Authorization header is invalid.");
                }

                // Read the token
                _jwtSecurityTokenHandlerProvider.SetMapInboundClaims(false);
                var jwtSecurityToken = _jwtSecurityTokenHandlerProvider.ReadJwtToken(token);

                if (jwtSecurityToken == default)
                {
                    throw new AuthStateException("The security token is invalid.");
                }

                externalId = jwtSecurityToken.Subject;
                externalSystem = jwtSecurityToken.Issuer;
            }

            if (_authenticationConfiguration.ApprovedRedirectUrls == null)
            {
                throw new AuthStateException("The approved redirect URL list is empty.");
            }

            if (!_authenticationConfiguration.ApprovedRedirectUrls.Any(url => string.Equals(url, redirectUrl, StringComparison.OrdinalIgnoreCase)))
            {
                throw new AuthStateException("The redirect URL was not found in the list of approved redirect URLs.");
            }

            var state = HttpUtility.UrlDecode(request.Query[Constants.StateQueryParameter]);

            return new AuthState(
                externalId,
                externalSystem,
                _utcNowFunc() + Constants.AuthStateExpiry,
                new Uri(redirectUrl),
                state);
        }

        public async Task<AuthState> RetrieveAuthState(string nonce, CancellationToken cancellationToken)
        {
            var blobName = EnsureArg.IsNotNullOrWhiteSpace(nonce);

            // Get a reference to the blob
            BlobClient blobClient = _blobContainerClient.GetBlobClient(blobName);

            // Get the AuthState
            var response = await blobClient.DownloadContentAsync(cancellationToken);

            // deserialize the AuthState
            using StreamReader reader = new StreamReader(response.Value.Content.ToStream());
            string json = await reader.ReadToEndAsync();
            return AuthState.Parse(json);
        }

        public async Task<string> StoreAuthState(AuthState state, CancellationToken cancellationToken)
        {
            var nonce = GenerateNonce(Constants.NonceLength);

            // Get a reference to the blob
            BlobClient blobClient = _blobContainerClient.GetBlobClient(nonce);

            // store state to blob storage
            await blobClient.UploadAsync(new BinaryData(JsonConvert.SerializeObject(state)), true, cancellationToken);

            return nonce;
        }

        private string GenerateNonce(int length)
        {
            var nonce = new StringBuilder();
            for (var i = 0; i < length; i++)
            {
                nonce.Append(_base36Chars[RandomNumberGenerator.GetInt32(0, _base36Chars.Length)]);
            }

            return nonce.ToString();
        }
    }
}
