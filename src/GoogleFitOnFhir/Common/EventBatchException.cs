// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace GoogleFitOnFhir.Common
{
    public class EventBatchException : Exception
    {
        public EventBatchException(string message)
            : base(message)
        {
        }
    }
}
