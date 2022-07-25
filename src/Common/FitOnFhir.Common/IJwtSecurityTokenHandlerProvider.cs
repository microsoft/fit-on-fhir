// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Health.FitOnFhir.Common
{
    /// <summary>
    /// An interface which wraps around a <see cref="JwtSecurityTokenHandler"/>.
    /// Primarily meant to enable testing of the <see cref="JwtSecurityTokenHandler"/>.
    /// </summary>
    public interface IJwtSecurityTokenHandlerProvider
    {
        /// <summary>
        /// Converts a string into an instance of <see cref="JwtSecurityToken"/>.
        /// </summary>
        /// <param name="token">A 'JSON Web Token' (JWT) in JWS or JWE Compact Serialization Format.</param>
        /// <returns>A <see cref="JwtSecurityToken"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="token"/> is null or empty.</exception>
        /// <exception cref="ArgumentException">'token.Length' is greater than <see cref="TokenHandler.MaximumTokenSizeInBytes"/>.</exception>
        /// <remarks><para>If the <paramref name="token"/> is in JWE Compact Serialization format, only the protected header will
        /// be deserialized.</para> This method is unable to decrypt the payload.</remarks>
        JwtSecurityToken ReadJwtToken(string token);

        /// <summary>
        /// Validates a token.
        /// On a validation failure, no exception will be thrown; instead, the exception will be set in the returned
        /// TokenValidationResult.Exception property.
        /// Callers should always check the TokenValidationResult.IsValid property to verify the validity of the result.
        /// </summary>
        /// <param name="token">The token to be validated.</param>
        /// <param name="validationParameters">A <see cref="TokenValidationParameters"/> required for validation.</param>
        /// <returns>A <see cref="TokenValidationResult"/></returns>
        Task<TokenValidationResult> ValidateTokenAsync(string token, TokenValidationParameters validationParameters);

        /// <summary>
        /// Gets or sets the MapInboundClaims property which is used when determining whether or not to map claim
        /// types that are extracted when validating a <see cref="JwtSecurityToken"/>.
        /// </summary>
        /// <param name="mappingEnabled">If this is set to true, the Claim.Type is set to the JSON claim 'name'
        /// after translating using this mapping. Otherwise, no mapping occurs. The default value is true.</param>
        void SetMapInboundClaims(bool mappingEnabled);
    }
}
