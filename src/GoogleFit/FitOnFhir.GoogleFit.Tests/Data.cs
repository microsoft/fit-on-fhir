// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IdentityModel.Tokens.Jwt;
using Google.Apis.Fitness.v1.Data;
using Microsoft.Health.FitOnFhir.Common;
using Microsoft.Health.FitOnFhir.Common.Models;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Models;
using Microsoft.Health.FitOnFhir.GoogleFit.Client.Responses;
using Newtonsoft.Json;
using Claim = System.Security.Claims.Claim;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Tests
{
    public static class Data
    {
        public const string UserId = "TestUserId";
        public const string DeviceUid = "TestDeviceUid";
        public const string PackageName = "TestApplicationPackageName";
        public const string DataSourceId = "test:com.google.heart_rate.bpm:com.google.android.apps.fitness:user_input";
        public const string DataTypeName = "com.google.heart_rate.bpm";
        public const string AccessToken = "TestAccessToken";
        public const string RefreshToken = "TestRefreshToken";
        public const string Issuer = "TestIssuer";
        public const string GoogleUserId = "TestGoogleUserId";
        public const string PatientId = "12345678-9101-1121-3141-516171819202";
        public const string ExternalPatientId = "ExternalPatientId";
        public const string ExternalSystem = "ExternalSystem";
        public const string RedirectUrl = "http://localhost";
        public const string State = "State";
        public const string AuthorizationState = $"{{\"{Constants.ExternalIdQueryParameter}\":\"{ExternalPatientId}\", " +
                                                 $"\"{Constants.ExternalSystemQueryParameter}\":\"{ExternalSystem}\", " +
                                                 $"\"ExpirationTimeStamp\":\"1/12/2004 12:01:00 AM -05:00\", " +
                                                 $"\"{Constants.RedirectUrlQueryParameter}\":\"{RedirectUrl}\", " +
                                                 $"\"{Constants.StateQueryParameter}\":\"{State}\"}}";

        public static readonly AuthState StoredAuthState = JsonConvert.DeserializeObject<AuthState>(AuthorizationState);

        public static MedTechDataset GetMedTechDataset(string deviceUid = DeviceUid, string packageName = PackageName, int pointCount = 1)
        {
            var points = new List<DataPoint>();

            for (int i = 0; i < pointCount; i++)
            {
                points.Add(new DataPoint
                {
                    Value = new List<Value>() { new Value { FpVal = 60 + i } },
                    StartTimeNanos = 165213768021173708 + (i * 10000000000),
                    EndTimeNanos = 165213768021173708 + (i * 10000000000),
                    DataTypeName = DataTypeName,
                    ModifiedTimeMillis = 1652137680539 + (i * 1000),
                });
            }

            var dataset = new Dataset
            {
                DataSourceId = DataSourceId,
                Point = points,
                MinStartTimeNs = 1649715998308000000,
                MaxEndTimeNs = 1652307998308000000,
            };

            var dataSource = new Client.Models.DataSource(DataSourceId, deviceUid, packageName);

            return new MedTechDataset(dataset, dataSource);
        }

        public static AuthTokensResponse GetAuthTokensResponse(string accessToken = AccessToken, string refreshToken = RefreshToken)
        {
            return new AuthTokensResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                IdToken = new JwtSecurityToken(issuer: Issuer, claims: new List<Claim>() { new Claim("sub", GoogleUserId) }),
            };
        }
    }
}
