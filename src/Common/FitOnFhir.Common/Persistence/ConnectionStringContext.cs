// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.FitOnFhir.Common.Persistence
{
    public class ConnectionStringContext
    {
        public ConnectionStringContext(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public string ConnectionString { get; set; }
    }
}