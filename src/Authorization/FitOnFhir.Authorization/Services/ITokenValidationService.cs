// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FitOnFhir.Authorization.Services
{
    public interface ITokenValidationService
    {
        /// <summary>
        /// Validates an access token using the provided issuer's metadata.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/> containing the access token.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to cancel the process.</param>
        /// <returns>true if the token is validated, false if not.</returns>
        public Task<bool> ValidateToken(HttpRequest request, CancellationToken cancellationToken);
    }
}
