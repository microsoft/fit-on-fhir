// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IdentityModel.Tokens.Jwt;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Health.FitOnFhir.Common.Exceptions;
using Microsoft.Health.FitOnFhir.Common.Models;
using Microsoft.Health.FitOnFhir.Common.Providers;
using Microsoft.Health.FitOnFhir.Common.Services;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.FitOnFhir.Common.Tests
{
    public class AuthStateServiceTests : AuthenticationBaseTests
    {
        private IBlobContainerClientProvider _blobContainerClientProvider;
        private BlobContainerClient _blobContainerClient;
        private BlobClient _blobClient;
        private BlobDownloadResult _blobDownloadResult;
        private BinaryData _authStateBinaryData;
        private readonly Func<DateTimeOffset> _utcNowFunc;
        private readonly AuthStateService _authStateService;

        private static readonly DateTimeOffset Now =
            new DateTimeOffset(2004, 1, 12, 0, 0, 0, new TimeSpan(-5, 0, 0));

        private readonly DateTimeOffset _expiresAt = Now + Constants.AuthStateExpiry;

        public AuthStateServiceTests()
        {
            SetupConfiguration(false);

            // create new service for testing
            _utcNowFunc = Substitute.For<Func<DateTimeOffset>>();
            _utcNowFunc().Returns(Now);
            SetupBlobSubstitutes();
            _authStateService = new AuthStateService(AuthConfiguration, _blobContainerClientProvider, SecurityTokenHandlerProvider, _utcNowFunc);
        }

        protected string ExpectedBlobContainerName => "BlobContainerName";

        protected string ExpectedNonce => "Nonce";

        protected string AuthorizationState =>
            $"{{\"{Constants.ExternalIdQueryParameter}\":\"{ExpectedExternalIdentifier}\", " +
            $"\"{Constants.ExternalSystemQueryParameter}\":\"{ExpectedExternalSystem}\", " +
            $"\"ExpirationTimeStamp\":\"{_utcNowFunc()}\", " +
            $"\"{Constants.RedirectUrlQueryParameter}\":\"{ExpectedRedirectUrl}\", " +
            $"\"{Constants.StateQueryParameter}\":\"{ExpectedState}\"}}";

        protected AuthState StoredAuthState => JsonConvert.DeserializeObject<AuthState>(AuthorizationState);

        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public void GivenAnonymousLoginEnabledWithValidRequest_WhenCreateAuthStateCalled_ReturnsAuthStateWithQueryParams(bool includeStateQueryParam)
        {
            SetupConfiguration(true);
            SetupHttpRequest(ExpectedToken, includePatient: true, includeSystem: true, includeRedirectUrl: true, includeState: includeStateQueryParam);

            AuthState authState = _authStateService.CreateAuthState(Request);

            Assert.Equal(ExpectedExternalSystem, authState.ExternalSystem);
            Assert.Equal(ExpectedExternalIdentifier, authState.ExternalIdentifier);
            Assert.Equal(_expiresAt, authState.ExpirationTimeStamp);
            Assert.Equal(ExpectedRedirectUrl, authState.RedirectUrl.ToString());
            if (includeStateQueryParam)
            {
                Assert.Equal(ExpectedState, authState.State);
            }
            else
            {
                Assert.Null(authState.State);
            }
        }

        [InlineData(false, true, true, "external_id")]
        [InlineData(false, true, false, "external_id, redirect_url")]
        [InlineData(false, false, true, "external_id, external_system")]
        [InlineData(false, false, false, "external_id, external_system, redirect_url")]
        [InlineData(true, true, false, "redirect_url")]
        [InlineData(true, false, true, "external_system")]
        [InlineData(true, false, false, "external_system, redirect_url")]
        [Theory]
        public void GivenAnonymousLoginEnabledWithInvalidRequest_WhenCreateAuthStateCalled_ThrowsAuthStateExceptionWithCorrectMessage(
            bool includePatient,
            bool includeSystem,
            bool includeRedirectUrl,
            string missing)
        {
            SetupConfiguration(true);
            SetupHttpRequest(ExpectedToken, includePatient: includePatient, includeSystem: includeSystem, includeRedirectUrl: includeRedirectUrl);

            try
            {
                AuthState authState = _authStateService.CreateAuthState(Request);
                Assert.Fail("Expected exception not thrown."); // CreateAuthState() did not throw an exception
            }
            catch (AuthStateException ex)
            {
                string errorMessageFormat = "{0}, {1}, {2} are required query parameters with anonymous authorization. The request is missing {3}.";
                string expectedMessage = string.Format(errorMessageFormat, Constants.ExternalIdQueryParameter, Constants.ExternalSystemQueryParameter, Constants.RedirectUrlQueryParameter, missing);
                Assert.Equal(expectedMessage, ex.Message);
            }
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public void GivenAnonymousLoginDisabledWithValidRequest_WhenCreateAuthStateCalled_ReturnsAuthStateWithSubjectAndIssuer(bool includeStateQueryParam)
        {
            SetupHttpRequest(ExpectedToken, includeRedirectUrl: true, includeState: includeStateQueryParam);

            AuthState authState = _authStateService.CreateAuthState(Request);

            Assert.Equal(ExpectedIssuer, authState.ExternalSystem);
            Assert.Equal(ExpectedSubject, authState.ExternalIdentifier);
            Assert.Equal(_expiresAt, authState.ExpirationTimeStamp);
            Assert.Equal(ExpectedRedirectUrl, authState.RedirectUrl.ToString());
            if (includeStateQueryParam)
            {
                Assert.Equal(ExpectedState, authState.State);
            }
            else
            {
                Assert.Null(authState.State);
            }
        }

        [InlineData(true, false, "external_id")]
        [InlineData(false, true, "external_system")]
        [InlineData(true, true, "external_id, external_system")]
        [Theory]
        public void GivenAnonymousLoginDisabledWithExternalQueryParametersInRequest_WhenCreateAuthStateCalled_ThrowsAuthStateExceptionWithCorrectMessage(bool includePatient, bool includeSystem, string present)
        {
            SetupHttpRequest(ExpectedToken, includePatient: includePatient, includeSystem: includeSystem, includeRedirectUrl: true);

            try
            {
                AuthState authState = _authStateService.CreateAuthState(Request);
                Assert.Fail("Expected exception not thrown."); // CreateAuthState() did not throw an exception
            }
            catch (AuthStateException ex)
            {
                string errorMessageFormat = "{0} and {1} are forbidden query parameters with authenticated authorization. The request contains {2}.";
                string expectedMessage = string.Format(errorMessageFormat, Constants.ExternalIdQueryParameter, Constants.ExternalSystemQueryParameter, present);
                Assert.Equal(expectedMessage, ex.Message);
            }
        }

        [Fact]
        public void GivenRedirectUrlNotInApprovedList_WhenCreateAuthStateCalled_ThrowsAuthStateExceptionWithCorrectMessage()
        {
            SetupHttpRequest(ExpectedToken);
            AuthConfiguration.ApprovedRedirectUrls.Returns(new List<string>());

            try
            {
                AuthState authState = _authStateService.CreateAuthState(Request);
                Assert.Fail("Expected exception not thrown."); // CreateAuthState() did not throw an exception
            }
            catch (AuthStateException ex)
            {
                Assert.Equal("The redirect URL was not found in the list of approved redirect URLs.", ex.Message);
            }
        }

        [Fact]
        public void GivenRedirectUrlNotInRequest_WhenCreateAuthStateCalled_ThrowsAuthStateExceptionWithCorrectMessage()
        {
            SetupHttpRequest(ExpectedToken, includeRedirectUrl: false);

            try
            {
                AuthState authState = _authStateService.CreateAuthState(Request);
                Assert.Fail("Expected exception not thrown."); // CreateAuthState() did not throw an exception
            }
            catch (AuthStateException ex)
            {
                Assert.Equal($"The required parameter {Constants.RedirectUrlQueryParameter} was not provided in the request.", ex.Message);
            }
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public void GivenApprovedRedirectUrlListReturnsNull_WhenCreateAuthStateCalled_ThrowsAuthStateExceptionWithCorrectMessage(bool anonymous)
        {
            SetupConfiguration(anonymous);
            List<string> emptyRedirectList = null;
            AuthConfiguration.ApprovedRedirectUrls.Returns(emptyRedirectList);

            if (anonymous)
            {
                SetupHttpRequest(ExpectedToken, includePatient: true, includeSystem: true);
            }
            else
            {
                SetupHttpRequest(ExpectedToken);
            }

            try
            {
                AuthState authState = _authStateService.CreateAuthState(Request);
                Assert.Fail("Expected exception not thrown."); // CreateAuthState() did not throw an exception
            }
            catch (AuthStateException ex)
            {
                Assert.Equal("The approved redirect URL list is empty.", ex.Message);
            }
        }

        [Fact]
        public void GivenHttpRequestIsNull_WhenCreateAuthStateCalled_ThrowsArgumentNullException()
        {
            HttpRequest nullHttpRequest = null;

            Assert.Throws<ArgumentNullException>(() => _authStateService.CreateAuthState(nullHttpRequest));
        }

        [Fact]
        public void GivenHttpRequestHasInvalidToken_WhenValidateTokenIsCalled_ThrowsAuthStateExceptionWithCorrectMessage()
        {
            SetupHttpRequest(string.Empty);

            try
            {
                AuthState authState = _authStateService.CreateAuthState(Request);
                Assert.Fail("Expected exception not thrown."); // CreateAuthState() did not throw an exception
            }
            catch (AuthStateException ex)
            {
                Assert.Equal("The request Authorization header is invalid.", ex.Message);
            }
        }

        [Fact]
        public void GivenReadJwtTokenReturnsDefault_WhenCreateAuthStateIsCalled_ThrowsAuthStateExceptionWithCorrectMessage()
        {
            SetupHttpRequest(ExpectedToken);

            JwtSecurityToken jwtSecurityToken = default;
            SecurityTokenHandlerProvider.ReadJwtToken(Arg.Is<string>(str => str == ExpectedToken)).Returns(jwtSecurityToken);

            try
            {
                AuthState authState = _authStateService.CreateAuthState(Request);
                Assert.Fail("Expected exception not thrown."); // CreateAuthState() did not throw an exception
            }
            catch (AuthStateException ex)
            {
                Assert.Equal("The security token is invalid.", ex.Message);
            }
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

            _blobClient.DownloadContentAsync(Arg.Any<CancellationToken>()).Throws(exception);
            await Assert.ThrowsAsync<Exception>(() => _authStateService.RetrieveAuthState(ExpectedNonce, CancellationToken.None));
        }

        [Fact]
        public async Task GivenNonceIsValid_WhenRetrieveAuthStateIsCalled_ReturnsStoredAuthState()
        {
            AuthState authState = await _authStateService.RetrieveAuthState(ExpectedNonce, CancellationToken.None);
            Assert.Equal(ExpectedExternalIdentifier, authState.ExternalIdentifier);
            Assert.Equal(ExpectedExternalSystem, authState.ExternalSystem);
        }

        [Fact]
        public async Task GivenGetBlobClientThrowsException_WhenStoreAuthStateIsCalled_ExceptionIsThrown()
        {
            _blobContainerClient.GetBlobClient(Arg.Any<string>()).Throws(new FileNotFoundException());
            await Assert.ThrowsAsync<FileNotFoundException>(() => _authStateService.StoreAuthState(StoredAuthState, CancellationToken.None));
        }

        [Fact]
        public async Task GivenUploadAsyncThrowsException_WhenStoreAuthStateIsCalled_ExceptionIsThrown()
        {
            _blobContainerClient.GetBlobClient(Arg.Any<string>()).Returns(_blobClient);
            _blobClient.UploadAsync(
                Arg.Any<BinaryData>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>()).Throws(new EndOfStreamException());

            await Assert.ThrowsAsync<EndOfStreamException>(() => _authStateService.StoreAuthState(StoredAuthState, CancellationToken.None));
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
            _blobContainerClient = Substitute.For<BlobContainerClient>();
            _blobContainerClientProvider = Substitute.For<IBlobContainerClientProvider>();
            _blobContainerClientProvider.GetBlobContainerClient(Arg.Any<string>()).Returns(_blobContainerClient);

            _blobClient = Substitute.For<BlobClient>();
            _blobContainerClient.GetBlobClient(Arg.Is<string>(str => str == ExpectedNonce)).Returns(_blobClient);

            _authStateBinaryData = new BinaryData(AuthorizationState);
            _blobDownloadResult = BlobsModelFactory.BlobDownloadResult(content: _authStateBinaryData);
            Response<BlobDownloadResult> blobDownloadInfoResponse = Substitute.For<Response<BlobDownloadResult>>();
            blobDownloadInfoResponse.Value.Returns(_blobDownloadResult);

            _blobClient.DownloadContentAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<Response<BlobDownloadResult>>(blobDownloadInfoResponse));
        }
    }
}
