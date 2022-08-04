// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IdentityModel.Tokens.Jwt;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Health.FitOnFhir.Common.Config;
using Microsoft.Health.FitOnFhir.Common.Exceptions;
using Microsoft.Health.FitOnFhir.Common.Models;
using Microsoft.Health.FitOnFhir.Common.Services;
using Microsoft.Health.FitOnFhir.Common.Tests.Mocks;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.FitOnFhir.Common.Tests
{
    public class AuthStateServiceTests : AuthenticationBaseTests, IDisposable
    {
        private BlobContainerClient _blobContainerClient;
        private BlobClient _blobClient;
        private BlobDownloadInfo _blobDownloadInfo;
        private Stream _authStateStream;
        private StreamWriter _authStateWriter;
        private readonly AzureConfiguration _azureConfiguration;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly MockLogger<AuthStateService> _logger;
        private readonly AuthStateService _authStateService;

        public AuthStateServiceTests()
        {
            SetupConfiguration(false);

            // create new service for testing
            _azureConfiguration = Substitute.For<AzureConfiguration>();
            _blobServiceClient = Substitute.For<BlobServiceClient>();
            _logger = Substitute.For<MockLogger<AuthStateService>>();
            SetupBlobSubstitutes();
            _authStateService = new AuthStateService(
                _azureConfiguration,
                AuthConfiguration,
                SecurityTokenHandlerProvider,
                _blobServiceClient,
                _logger);
        }

        protected string ExpectedBlobContainerName => "BlobContainerName";

        protected string ExpectedNonce => "Nonce";

        protected string AuthorizationState =>
            $"{{\"{Constants.ExternalIdQueryParameter}\":\"{ExpectedExternalIdentifier}\", " +
            $"\"{Constants.ExternalSystemQueryParameter}\":\"{ExpectedExternalSystem}\", " +
            $"\"{Constants.RedirectUrlQueryParameter}\":\"{ExpectedRedirectUrl}\", " +
            $"\"{Constants.StateQueryParameter}\":\"{ExpectedState}\"}}";

        protected AuthState StoredAuthState => JsonConvert.DeserializeObject<AuthState>(AuthorizationState);

        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public void GivenAnonymousLoginEnabledWithValidRequest_WhenCreateAuthStateCalled_ReturnsAuthStateWithQueryParams(bool includeStateQueryParam)
        {
            SetupConfiguration(true);
            SetupHttpRequest(ExpectedToken, true, includeStateQueryParam);

            var authState = _authStateService.CreateAuthState(Request);

            Assert.Equal(ExpectedExternalSystem, authState.ExternalSystem);
            Assert.Equal(ExpectedExternalIdentifier, authState.ExternalIdentifier);
            Assert.Equal(ExpectedRedirectUrl, authState.RedirectUrl);
            if (includeStateQueryParam)
            {
                Assert.Equal(ExpectedState, authState.State);
            }
            else
            {
                Assert.Null(authState.State);
            }
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public void GivenAnonymousLoginDisabledWithValidRequest_WhenCreateAuthStateCalled_ReturnsAuthStateWithSubjectAndIssuer(bool includeStateQueryParam)
        {
            SetupHttpRequest(ExpectedToken, includeState: includeStateQueryParam);

            var authState = _authStateService.CreateAuthState(Request);

            Assert.Equal(ExpectedIssuer, authState.ExternalSystem);
            Assert.Equal(ExpectedSubject, authState.ExternalIdentifier);
            Assert.Equal(ExpectedRedirectUrl, authState.RedirectUrl);
            if (includeStateQueryParam)
            {
                Assert.Equal(ExpectedState, authState.State);
            }
            else
            {
                Assert.Null(authState.State);
            }
        }

        [Fact]
        public void GivenAnonymousLoginDisabledWithExternalQueryParametersInRequest_WhenCreateAuthStateCalled_ThrowsArgumentException()
        {
            SetupHttpRequest(ExpectedToken, true);

            Assert.Throws<ArgumentException>(() => _authStateService.CreateAuthState(Request));

            _logger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                Arg.Is<string>(msg =>
                    msg == $"{Constants.ExternalIdQueryParameter} and {Constants.ExternalSystemQueryParameter} are forbidden query parameters with non-anonymous authorization."));
        }

        [Fact]
        public void GivenRedirectUrlNotInApprovedList_WhenCreateAuthStateCalled_ThrowsRedirectUrlExceptionAndErrorIsLogged()
        {
            SetupHttpRequest(ExpectedToken);
            AuthConfiguration.ApprovedRedirectUrls.Returns(new List<string>());

            Assert.Throws<RedirectUrlException>(() => _authStateService.CreateAuthState(Request));

            _logger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                Arg.Is<string>(msg =>
                    msg == $"{ExpectedRedirectUrl} was not found in the list of approved redirect URLs."));
        }

        [Fact]
        public void GivenHttpRequestHasInvalidToken_WhenValidateTokenIsCalled_ReturnsDefaultAndErrorIsLogged()
        {
            SetupHttpRequest(string.Empty);

            var authState = _authStateService.CreateAuthState(Request);

            Assert.Null(authState);
            _logger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                Arg.Is<string>(msg => msg == "The request Authorization header is invalid."));
        }

        [Fact]
        public void GivenReadJwtTokenReturnsDefault_WhenValidateTokenIsCalled_ReturnsDefaultAndErrorIsLogged()
        {
            SetupHttpRequest(ExpectedToken);

            JwtSecurityToken jwtSecurityToken = default;
            SecurityTokenHandlerProvider.ReadJwtToken(Arg.Is<string>(str => str == ExpectedToken)).Returns(jwtSecurityToken);

            var authState = _authStateService.CreateAuthState(Request);

            Assert.Null(authState);
        }

        [Fact]
        public async Task GivenNonceIsNull_WhenRetrieveAuthStateIsCalled_ThrowsNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _authStateService.RetrieveAuthState(null, CancellationToken.None));
        }

        [Fact]
        public async Task GivenNonceIsEmpty_WhenRetrieveAuthStateIsCalled_ThrowsNullException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _authStateService.RetrieveAuthState(string.Empty, CancellationToken.None));
        }

        [Fact]
        public async Task GivenGetBlobClientThrowsException_WhenRetrieveAuthStateIsCalled_ThrowsException()
        {
            string exceptionMessage = "GetBlobClient exception";
            var exception = new Exception(exceptionMessage);

            _blobContainerClient.GetBlobClient(Arg.Is<string>(str => str == ExpectedNonce)).Throws(exception);
            await Assert.ThrowsAsync<Exception>(() => _authStateService.RetrieveAuthState(ExpectedNonce, CancellationToken.None));
        }

        [Fact]
        public async Task GivenDownloadAsyncThrowsException_WhenRetrieveAuthStateIsCalled_ThrowsException()
        {
            string exceptionMessage = "DownloadAsync exception";
            var exception = new Exception(exceptionMessage);

            _blobClient.DownloadAsync(Arg.Any<CancellationToken>()).Throws(exception);
            await Assert.ThrowsAsync<Exception>(() => _authStateService.RetrieveAuthState(ExpectedNonce, CancellationToken.None));
        }

        [Fact]
        public async Task GivenNonceIsValid_WhenRetrieveAuthStateIsCalled_RetrunsStoredAuthState()
        {
            var authState = await _authStateService.RetrieveAuthState(ExpectedNonce, CancellationToken.None);
            Assert.Equal(ExpectedExternalIdentifier, authState.ExternalIdentifier);
            Assert.Equal(ExpectedExternalSystem, authState.ExternalSystem);
        }

        [Fact]
        public async Task GivenGetBlobClientThrowsException_WhenStoreAuthStateIsCalled_ExceptionIsLoggedAndNullIsReturned()
        {
            string exceptionMessage = "Failed to store the auth state.";
            var exception = new Exception(exceptionMessage);

            _blobContainerClient.GetBlobClient(Arg.Any<string>()).Throws(exception);

            var nonce = await _authStateService.StoreAuthState(StoredAuthState, CancellationToken.None);

            _logger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                Arg.Any<Exception>(),
                Arg.Is<string>(msg => msg == exceptionMessage));
            Assert.Null(nonce);
        }

        [Fact]
        public async Task GivenUploadAsyncThrowsException_WhenStoreAuthStateIsCalled_ExceptionIsLoggedAndNullIsReturned()
        {
            string exceptionMessage = "Failed to store the auth state.";
            var exception = new Exception(exceptionMessage);

            _blobContainerClient.GetBlobClient(Arg.Any<string>()).Returns(_blobClient);
            _blobClient.UploadAsync(
                Arg.Any<BinaryData>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>()).Throws(exception);

            var nonce = await _authStateService.StoreAuthState(StoredAuthState, CancellationToken.None);

            _logger.Received(1).Log(
                Arg.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                Arg.Any<Exception>(),
                Arg.Is<string>(msg => msg == exceptionMessage));
            Assert.Null(nonce);
        }

        [Fact]
        public async Task GivenAuthStateIsValid_WhenStoreAuthStateIsCalled_UploadAsyncIsCalled()
        {
            _blobContainerClient.GetBlobClient(Arg.Any<string>()).Returns(_blobClient);
            var nonce = await _authStateService.StoreAuthState(StoredAuthState, CancellationToken.None);

            await _blobClient.Received(1).UploadAsync(
                Arg.Any<BinaryData>(),
                Arg.Is<bool>(b => b == true),
                Arg.Any<CancellationToken>());

            // verify nonce length is 24 chars
            Assert.Equal(24, nonce.Length);
        }

        private void SetupBlobSubstitutes()
        {
            _azureConfiguration.BlobContainerName = ExpectedBlobContainerName;

            _blobContainerClient = Substitute.For<BlobContainerClient>();
            _blobServiceClient.GetBlobContainerClient(Arg.Is<string>(str => str == ExpectedBlobContainerName))
                .Returns(_blobContainerClient);

            _blobClient = Substitute.For<BlobClient>();
            _blobContainerClient.GetBlobClient(Arg.Is<string>(str => str == ExpectedNonce)).Returns(_blobClient);

            _authStateStream = BuildAuthStream(AuthorizationState);
            _blobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(content: _authStateStream);
            var blobDownloadInfoResponse = Substitute.For<Response<BlobDownloadInfo>>();
            blobDownloadInfoResponse.Value.Returns(_blobDownloadInfo);

            _blobClient.DownloadAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<Response<BlobDownloadInfo>>(blobDownloadInfoResponse));
        }

        private Stream BuildAuthStream(string serializedAuthState)
        {
            var stream = new MemoryStream();
            _authStateWriter = new StreamWriter(stream);
            _authStateWriter.Write(serializedAuthState);
            _authStateWriter.Flush();
            stream.Position = 0;
            return stream;
        }

        public void Dispose()
        {
            _authStateStream.Dispose();
            _authStateWriter.Dispose();
        }
    }
}
