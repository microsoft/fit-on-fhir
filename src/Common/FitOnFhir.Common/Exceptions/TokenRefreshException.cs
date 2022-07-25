// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.FitOnFhir.Common.Exceptions
{
    public class TokenRefreshException : Exception
    {
        public TokenRefreshException(string message)
            : base(message)
        {
        }
    }
}
