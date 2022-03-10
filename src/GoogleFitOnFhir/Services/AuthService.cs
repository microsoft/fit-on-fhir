// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Fitness.v1;
using GoogleFitOnFhir.Clients.GoogleFit;
using GoogleFitOnFhir.Clients.GoogleFit.Requests;
using GoogleFitOnFhir.Clients.GoogleFit.Responses;

namespace GoogleFitOnFhir.Services
{
    public class AuthService : IAuthService
    {
        private readonly ClientContext _clientContext;

        public AuthService(ClientContext clientContext)
        {
            _clientContext = clientContext;
        }

        public Task<AuthUriResponse> AuthUriRequest()
        {
            return new AuthUriRequest(_clientContext, GetAuthFlow())
                .ExecuteAsync();
        }

        public Task<AuthTokensResponse> AuthTokensRequest(string authCode)
        {
            return new AuthTokensRequest(_clientContext, authCode, GetAuthFlow())
                .ExecuteAsync();
        }

        public Task<AuthTokensResponse> RefreshTokensRequest(string refreshToken)
        {
            return new RefreshTokensRequest(_clientContext, refreshToken, GetAuthFlow())
                .ExecuteAsync();
        }

        private IAuthorizationCodeFlow GetAuthFlow()
        {
            // TODO: Customize datastore to use KeyVault
            return new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _clientContext.ClientId,
                    ClientSecret = _clientContext.ClientSecret,
                },

                // TODO: Only need write scopes for e2e tests - make this dynamic
                Scopes = new[]
                {
                    "https://www.googleapis.com/auth/userinfo.email",
                    "https://www.googleapis.com/auth/userinfo.profile",
                    FitnessService.Scope.FitnessBloodGlucoseRead,
                    FitnessService.Scope.FitnessBloodGlucoseWrite,
                    FitnessService.Scope.FitnessHeartRateRead,
                    FitnessService.Scope.FitnessHeartRateWrite,
                },
            });
        }
    }
}