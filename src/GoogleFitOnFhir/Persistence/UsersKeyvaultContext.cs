// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace GoogleFitOnFhir.Persistence
{
    public class UsersKeyVaultContext
    {
        public UsersKeyVaultContext(string uriString)
        {
            Uri = new Uri(uriString);
        }

        public Uri Uri { get; set; }
    }
}