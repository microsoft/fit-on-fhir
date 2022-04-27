// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace GoogleFitOnFhir.Services
{
    public abstract class AuthService<TServiceType>
        where TServiceType : class
    {
        protected AuthService(ILogger<TServiceType> logger)
        {
            Logger = logger;
        }

        protected ILogger<TServiceType> Logger { get; }
    }
}