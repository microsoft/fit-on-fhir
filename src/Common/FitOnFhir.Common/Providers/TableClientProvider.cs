// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Data.Tables;
using EnsureThat;
using Microsoft.Health.FitOnFhir.Common.Config;

namespace Microsoft.Health.FitOnFhir.Common.Providers
{
    public class TableClientProvider : CredentialedProvider, ITableClientProvider
    {
        private readonly Uri _tableServiceUri;

        public TableClientProvider(AzureConfiguration azureConfiguration, ITokenCredentialProvider tokenCredentialProvider)
            : base(tokenCredentialProvider)
        {
            _tableServiceUri = EnsureArg.IsNotNull(azureConfiguration?.TableServiceUri, nameof(azureConfiguration.TableServiceUri));
        }

        public TableClient GetTableClient(string tableName)
        {
            EnsureArg.IsNotNullOrWhiteSpace(tableName, nameof(tableName));

            return new TableClient(_tableServiceUri, tableName, GetTokenCredential());
        }
    }
}
