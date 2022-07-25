// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Health.FitOnFhir.Common.Interfaces
{
    public interface IStartupBase
    {
        /// <summary>
        /// Configure method which passes an <see cref="IConfiguration"/> object with all environment variables assigned.
        /// </summary>
        /// <param name="builder">The <see cref="IFunctionsHostBuilder"/> provided.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> which contains the environment variables.</param>
        void Configure(IFunctionsHostBuilder builder, IConfiguration configuration);
    }
}
