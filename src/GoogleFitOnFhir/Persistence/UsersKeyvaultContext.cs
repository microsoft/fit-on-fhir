// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace GoogleFitOnFhir.Persistence
{
    public class UsersKeyvaultContext
    {
        public UsersKeyvaultContext(string uri)
        {
            Uri = uri;
        }

        public string Uri { get; set; }
    }
}